using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Subjects;

using DynamicData;

using Microsoft.Diagnostics.Tracing.AutomatedAnalysis;

namespace ReactiveCollectionsTest.DynamicDataChangeSets
{
    public sealed class ObservableDictionary<TKey, TValue>
            : DisposableBase,
                IObservableDictionary<TKey, TValue>,
                IObservableReadOnlyDictionary<TKey, TValue>
        where TKey : notnull
        where TValue : notnull
    {
        public ObservableDictionary(int capacity)
        {
            _changeSets         = new();
            _collectionChanged  = new();
            _itemsByKey         = new(capacity);
        }

        public ObservableDictionary()
        {
            _changeSets         = new();
            _collectionChanged  = new();
            _itemsByKey         = new();
        }

        public TValue this[TKey key]
        {
            get => _itemsByKey[key];
            set 
            {
                var changeSet = _changeSets.HasObservers
                    ? new ChangeSet<TValue, TKey>(capacity: 1)
                    {
                        _itemsByKey.TryGetValue(key, out var oldValue)
                            ? new(
                                reason:     ChangeReason.Update,
                                key:        key,
                                current:    value,
                                previous:   oldValue)
                            : new(
                                reason:     ChangeReason.Add,
                                key:        key,
                                current:    value)
                    }
                    : null;

                _itemsByKey[key] = value;

                if (changeSet is not null)
                    _changeSets.OnNext(changeSet);
                _collectionChanged.OnNext(Unit.Default);
            }
        }

        public IObservable<Unit> CollectionChanged
            => _collectionChanged;

        public int Count
            => _itemsByKey.Count;

        public IReadOnlyCollection<TKey> Keys
            => _itemsByKey.Keys;

        public IReadOnlyCollection<TValue> Values
            => _itemsByKey.Values;

        public void Add(TKey key, TValue value)
        {
            _itemsByKey.Add(key, value);

            if (_changeSets.HasObservers)
                _changeSets.OnNext(new ChangeSet<TValue, TKey>(capacity: 1)
                {
                    new(reason:     ChangeReason.Add,
                        key:        key,
                        current:    value)
                });
            _collectionChanged.OnNext(Unit.Default);
        }

        public void AddRange(
            IEnumerable<TValue> values,
            Func<TValue, TKey>  keySelector)
        {
            if(values.TryGetNonEnumeratedCount(out var valuesCount) && (valuesCount is 0))
                return;

            AddRange_Internal(
                values:         values,
                valuesCount:    valuesCount,
                keySelector:    keySelector);
        }

        public void ApplyChangeSet(IChangeSet<TValue, TKey> changeSet)
        {
            if (changeSet is ChangeSet<TValue, TKey> concreteChangeSet)
                foreach(var change in concreteChangeSet)
                    ApplyChange(change, nameof(changeSet));
            else
                foreach(var change in changeSet)
                    ApplyChange(change, nameof(changeSet));

            _changeSets.OnNext(changeSet);
            _collectionChanged.OnNext(Unit.Default);
        }

        public void Clear()
        {
            if (_itemsByKey.Count is 0)
                return;

            var changeSet = null as ChangeSet<TValue, TKey>;
            if (_changeSets.HasObservers)
            {
                changeSet = new(capacity: _itemsByKey.Count);
                foreach(var pair in _itemsByKey)
                    changeSet.Add(new(
                        reason:     ChangeReason.Remove,
                        key:        pair.Key,
                        current:    pair.Value));
            }

            _itemsByKey.Clear();

            if (changeSet is not null)
                _changeSets.OnNext(changeSet);
            _collectionChanged.OnNext(Unit.Default);
        }

        public bool ContainsKey(TKey key)
            => _itemsByKey.ContainsKey(key);

        public Dictionary<TKey, TValue>.Enumerator GetEnumerator()
            => _itemsByKey.GetEnumerator();

        public IObservable<TValue> ObserveValue(TKey key)
            => System.Reactive.Linq.Observable.Create<TValue>(observer =>
            {
                if (!_itemsByKey.TryGetValue(key, out var initialValue))
                {
                    observer.OnCompleted();
                    return Disposable.Empty;
                }
                
                observer.OnNext(initialValue);

                return _changeSets.Subscribe(
                    onNext:         changeSet =>
                    {
                        if (changeSet is ChangeSet<TValue, TKey> concreteChangeSet)
                        {
                            foreach(var change in concreteChangeSet)
                                if (!ProcessChange(observer, key, change))
                                    break;
                        }
                        else
                        {
                            foreach(var change in changeSet)
                                if (!ProcessChange(observer, key, change))
                                    break;
                        }
                    },
                    onError:        observer.OnError,
                    onCompleted:    observer.OnCompleted);

                static bool ProcessChange(
                    IObserver<TValue>       observer,
                    TKey                    key,
                    Change<TValue, TKey>    change)
                {
                    switch(change.Reason)
                    {
                        case ChangeReason.Add:
                            if (EqualityComparer<TKey>.Default.Equals(change.Key, key))
                                throw new InvalidOperationException($"Unable to process {nameof(Change<TValue, TKey>)} reason {nameof(ChangeReason.Add)}: Key is already present in the collection");
                            break;

                        case ChangeReason.Moved:
                        case ChangeReason.Refresh:
                            break;

                        case ChangeReason.Remove:
                            if (EqualityComparer<TKey>.Default.Equals(change.Key, key))
                            {
                                observer.OnCompleted();
                                return false;
                            }
                            break;

                        case ChangeReason.Update:
                            if (EqualityComparer<TKey>.Default.Equals(change.Key, key))
                            {
                                observer.OnNext(change.Current);
                                return false;
                            }
                            break;

                        default:
                            throw new InvalidOperationException($"Unsupported {nameof(Change<TValue, TKey>)} reason {change.Reason} in {nameof(IChangeSet<TValue, TKey>)}");
                    }

                    return true;
                }
            });

        public bool Remove(TKey key)
        {
            if (!_itemsByKey.Remove(key, out var value))
                return false;

            if (_changeSets.HasObservers)
                _changeSets.OnNext(new ChangeSet<TValue, TKey>(capacity: 1)
                {
                    new(reason:     ChangeReason.Remove,
                        key:        key,
                        current:    value)
                });
            _collectionChanged.OnNext(Unit.Default);

            return true;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (!((ICollection<KeyValuePair<TKey, TValue>>)_itemsByKey).Remove(item))
                return false;

            if (_changeSets.HasObservers)
                _changeSets.OnNext(new ChangeSet<TValue, TKey>(capacity: 1)
                {
                    new(reason:     ChangeReason.Remove,
                        key:        item.Key,
                        current:    item.Value)
                });
            _collectionChanged.OnNext(Unit.Default);

            return true;
        }

        public void Reset(
            IEnumerable<TValue> values,
            Func<TValue, TKey>  keySelector)
        {
            if (values.TryGetNonEnumeratedCount(out var valuesCount))
            {
                if ((valuesCount is 0) && (_itemsByKey.Count is 0))
                    return;
            
                if (valuesCount is 0)
                {
                    Clear();
                    return;
                }
            }
            
            if (_itemsByKey.Count is 0)
            {
                AddRange_Internal(
                    values:         values,
                    valuesCount:    valuesCount,
                    keySelector:    keySelector);
                return;
            }

            if (_changeSets.HasObservers)
            {
                var changeSet = new ChangeSet<TValue, TKey>(capacity: _itemsByKey.Count + valuesCount);

                foreach(var pair in _itemsByKey)
                    changeSet.Add(new(
                        reason:     ChangeReason.Remove,
                        key:        pair.Key,
                        current:    pair.Value));

                _itemsByKey.Clear();

                foreach(var newValue in values)
                {
                    var key = keySelector.Invoke(newValue);
                    _itemsByKey.Add(key, newValue);
                    changeSet.Add(new(
                        reason:     ChangeReason.Add,
                        key:        key,
                        current:    newValue));
                }

                _changeSets.OnNext(changeSet);
            }
            else
            {
                _itemsByKey.Clear();
                foreach(var value in values)
                    _itemsByKey.Add(keySelector.Invoke(value), value);
            }

            _collectionChanged.OnNext(Unit.Default);
        }

        public IDisposable Subscribe(IObserver<IChangeSet<TValue, TKey>> observer)
        {
            if (_itemsByKey.Count is not 0)
            {
                var changeSet = new ChangeSet<TValue, TKey>(capacity: _itemsByKey.Count);
                foreach(var pair in _itemsByKey)
                    changeSet.Add(new(
                        reason:     ChangeReason.Add,
                        key:        pair.Key,
                        current:    pair.Value));

                observer.OnNext(changeSet);
            }

            return _changeSets.Subscribe(observer);
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
            => _itemsByKey.TryGetValue(key, out value);

        protected override void OnDisposing(DisposalType type)
        {
            if (type is DisposalType.Managed)
            {
                _changeSets         .OnCompleted();
                _collectionChanged  .OnCompleted();

                _changeSets         .Dispose();
                _collectionChanged  .Dispose();
            }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
            => ((ICollection<KeyValuePair<TKey, TValue>>)_itemsByKey).IsReadOnly;

        ICollection<TKey> IDictionary<TKey, TValue>.Keys
            => _itemsByKey.Keys;

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
            => _itemsByKey.Keys;

        ICollection<TValue> IDictionary<TKey, TValue>.Values
            => _itemsByKey.Values;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
            => _itemsByKey.Values;

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
            => Add(item.Key, item.Value);

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
            => ((ICollection<KeyValuePair<TKey, TValue>>)_itemsByKey).Contains(item);

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
            => ((ICollection<KeyValuePair<TKey, TValue>>)_itemsByKey).CopyTo(array, arrayIndex);

        IEnumerator IEnumerable.GetEnumerator()
            => _itemsByKey.GetEnumerator();

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
            => ((IEnumerable<KeyValuePair<TKey, TValue>>)_itemsByKey).GetEnumerator();

        private void AddRange_Internal(
            IEnumerable<TValue> values,
            int                 valuesCount,
            Func<TValue, TKey>  keySelector)
        {
            _itemsByKey.EnsureCapacity(_itemsByKey.Count + valuesCount);

            if (_changeSets.HasObservers)
            {
                var changeSet = new ChangeSet<TValue, TKey>(capacity: valuesCount);

                foreach(var value in values)
                {
                    var key = keySelector.Invoke(value);
                    _itemsByKey.Add(key, value);
                    changeSet.Add(new(
                        reason:     ChangeReason.Add,
                        key:        key,
                        current:    value));
                }

                _changeSets.OnNext(changeSet);
            }
            else
                foreach(var value in values)
                    _itemsByKey.Add(keySelector.Invoke(value), value);

            _collectionChanged.OnNext(Unit.Default);
        }

        private void ApplyChange(
            Change<TValue, TKey>    change,
            string                  paramName)
        {
            switch(change.Reason)
            {
                case ChangeReason.Add:
                    _itemsByKey.Add(
                        key:    change.Key,
                        value:  change.Current);
                    break;

                case ChangeReason.Moved:
                case ChangeReason.Refresh:
                    break;

                case ChangeReason.Remove:
                    _itemsByKey.Remove(change.Key);
                    break;

                case ChangeReason.Update:
                    _itemsByKey[change.Key] = change.Current;
                    break;

                default:
                    throw new ArgumentException($"Unsupported {nameof(Change<TValue, TKey>)} reason {change.Reason} in {nameof(IChangeSet<TValue, TKey>)}", paramName);
            }
        }

        private readonly Subject<IChangeSet<TValue, TKey>>  _changeSets;
        private readonly Subject<Unit>                      _collectionChanged;
        private readonly Dictionary<TKey, TValue>           _itemsByKey;
    }
}
