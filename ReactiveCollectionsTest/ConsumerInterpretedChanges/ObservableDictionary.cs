using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Subjects;

namespace ReactiveCollectionsTest.ConsumerInterpretedChanges
{
    public sealed class ObservableDictionary<TKey, TValue>
            : DisposableBase,
                IObservableDictionary<TKey, TValue>,
                IObservableReadOnlyDictionary<TKey, TValue>
        where TKey : notnull
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
                KeyedChangeSet<TKey, TValue>? changeSet = _changeSets.HasObservers
                    ? _itemsByKey.TryGetValue(key, out var oldItem)
                        ? KeyedChangeSet.Replacement(
                            key:        key,
                            oldItem:    oldItem,
                            newItem:    value)
                        : KeyedChangeSet.Addition(
                            key:    key,
                            item:   value)
                    : null;

                _itemsByKey[key] = value;

                if (changeSet is not null)
                    _changeSets.OnNext(changeSet.Value);
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
                _changeSets.OnNext(KeyedChangeSet.Addition(
                    key:    key,
                    item:   value));
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

        public void ApplyChangeSet(KeyedChangeSet<TKey, TValue> changeSet)
        {
            switch(changeSet.Type)
            {
                case ChangeSetType.Clear:
                    _itemsByKey.Clear();
                    break;

                case ChangeSetType.Reset:
                    _itemsByKey.Clear();
                    foreach(var change in changeSet.Changes)
                        if (change.Type is KeyedChangeType.Addition)
                            _itemsByKey.Add(
                                key:    change.Key,
                                value:  change.NewItem.Value);
                    break;

                case ChangeSetType.Update:
                    foreach(var change in changeSet.Changes)
                    {
                        switch(change.Type)
                        {
                            case KeyedChangeType.Addition:
                                _itemsByKey.Add(
                                    key:    change.Key,
                                    value:  change.NewItem.Value);
                                break;

                            case KeyedChangeType.Removal:
                                _itemsByKey.Remove(change.Key);
                                break;

                            case KeyedChangeType.Replacement:
                                _itemsByKey[change.Key] = change.NewItem.Value;
                                break;

                            default:
                                throw new ArgumentException($"Unsupported {nameof(KeyedChange)} type {change.Type} in {nameof(KeyedChangeSet)}", nameof(changeSet));
                        }
                    }
                    break;

                default:
                    throw new ArgumentException($"Unsupported {nameof(KeyedChangeSet)} type {changeSet.Type}", nameof(changeSet));
            }

            _changeSets.OnNext(changeSet);
            _collectionChanged.OnNext(Unit.Default);
        }

        public void Clear()
        {
            if (_itemsByKey.Count is 0)
                return;

            KeyedChangeSet<TKey, TValue>? changeSet = _changeSets.HasObservers
                ? KeyedChangeSet.Clear(_itemsByKey)
                : null;

            _itemsByKey.Clear();

            if (changeSet is not null)
                _changeSets.OnNext(changeSet.Value);
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
                        switch(changeSet.Type)
                        {
                            case ChangeSetType.Clear:
                            case ChangeSetType.Reset:
                                observer.OnCompleted();
                                break;

                            case ChangeSetType.Update:
                                foreach(var change in changeSet.Changes)
                                    switch(change.Type)
                                    {
                                        case KeyedChangeType.Addition:
                                            if (EqualityComparer<TKey>.Default.Equals(change.Key, key))
                                                throw new InvalidOperationException($"Unable to process {nameof(KeyedChange)} type {nameof(KeyedChangeType.Addition)}: Key is already present in the collection");
                                            break;

                                        case KeyedChangeType.Removal:
                                            if (EqualityComparer<TKey>.Default.Equals(change.Key, key))
                                            {
                                                observer.OnCompleted();
                                                goto END_UPDATE;
                                            }
                                            break;

                                        case KeyedChangeType.Replacement:
                                            if (EqualityComparer<TKey>.Default.Equals(change.Key, key))
                                            {
                                                observer.OnNext(change.NewItem.Value);
                                                goto END_UPDATE;
                                            }
                                            break;

                                        default:
                                            throw new ArgumentException($"Unsupported {nameof(KeyedChange)} type {change.Type} in {nameof(KeyedChangeSet)}", nameof(changeSet));
                                    }
                                END_UPDATE:
                                break;

                            default:
                                throw new ArgumentException($"Unsupported {nameof(KeyedChangeSet)} type {changeSet.Type}", nameof(changeSet));
                        }
                    },
                    onError:        observer.OnError,
                    onCompleted:    observer.OnCompleted);
            });

        public bool Remove(TKey key)
        {
            if (!_itemsByKey.Remove(key, out var value))
                return false;

            if (_changeSets.HasObservers)
                _changeSets.OnNext(KeyedChangeSet.Removal(
                    key:    key,
                    item:   value));
            _collectionChanged.OnNext(Unit.Default);

            return true;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (!((ICollection<KeyValuePair<TKey, TValue>>)_itemsByKey).Remove(item))
                return false;

            if (_changeSets.HasObservers)
                _changeSets.OnNext(KeyedChangeSet.Removal(
                    key:    item.Key,
                    item:   item.Value));
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
                var oldItems = System.Collections.Immutable.ImmutableArray.CreateRange(_itemsByKey);
                var newItems = System.Collections.Immutable.ImmutableArray.CreateBuilder<KeyValuePair<TKey, TValue>>(initialCapacity: valuesCount);

                _itemsByKey.Clear();

                foreach(var newValue in values)
                {
                    var key = keySelector.Invoke(newValue);
                    _itemsByKey.Add(key, newValue);
                    newItems.Add(new(key, newValue));
                }

                _changeSets.OnNext(KeyedChangeSet.Reset(
                    oldItems:   oldItems,
                    newItems:   newItems.MoveToOrCreateImmutable()));
            }
            else
            {
                _itemsByKey.Clear();
                foreach(var value in values)
                    _itemsByKey.Add(keySelector.Invoke(value), value);
            }

            _collectionChanged.OnNext(Unit.Default);
        }

        public IDisposable Subscribe(IObserver<KeyedChangeSet<TKey, TValue>> observer)
        {
            if (_itemsByKey.Count is not 0)
                observer.OnNext(KeyedChangeSet.Addition(System.Collections.Immutable.ImmutableArray.CreateRange(_itemsByKey)));

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
                var additions = System.Collections.Immutable.ImmutableArray.CreateBuilder<KeyValuePair<TKey, TValue>>(initialCapacity: valuesCount);

                foreach(var value in values)
                {
                    var key = keySelector.Invoke(value);
                    _itemsByKey.Add(key, value);
                    additions.Add(new(key, value));
                }

                _changeSets.OnNext(KeyedChangeSet.Addition(additions.MoveToOrCreateImmutable()));
            }
            else
                foreach(var value in values)
                    _itemsByKey.Add(keySelector.Invoke(value), value);

            _collectionChanged.OnNext(Unit.Default);
        }

        private readonly Subject<KeyedChangeSet<TKey, TValue>>  _changeSets;
        private readonly Subject<Unit>                          _collectionChanged;
        private readonly Dictionary<TKey, TValue>               _itemsByKey;
    }
}
