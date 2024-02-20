using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Subjects;

namespace ReactiveCollectionsTest.PolymorphicChangeSets
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
                var changeSet = _changeSets.HasObservers
                    ? _itemsByKey.TryGetValue(key, out var oldItem)
                        ? new KeyedReplacementChangeSet<TKey, TValue>()
                        {
                            Key     = key,
                            NewItem = value,
                            OldItem = oldItem
                        }
                        : new KeyedAdditionChangeSet<TKey, TValue>()
                        {
                            Item    = value,
                            Key     = key
                        }
                    : null as IKeyedChangeSet<TKey, TValue>;

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
                _changeSets.OnNext(new KeyedAdditionChangeSet<TKey, TValue>()
                {
                    Item    = value,
                    Key     = key
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

        public void ApplyChangeSet(IKeyedChangeSet<TKey, TValue> changeSet)
        {
            switch(changeSet.Type)
            {
                case ChangeSetType.Clear:
                    _itemsByKey.Clear();
                    break;

                case ChangeSetType.Reset:
                    _itemsByKey.Clear();
                    foreach(var change in changeSet)
                        if (change.Type is KeyedChangeType.Addition)
                        {
                            var addition = change.AsAddition();
                            _itemsByKey.Add(
                                key:    addition.Key,
                                value:  addition.Item);
                        }
                    break;

                case ChangeSetType.Update:
                    foreach(var change in changeSet)
                    {
                        switch(change.Type)
                        {
                            case KeyedChangeType.Addition:
                                var addition = change.AsAddition();
                                _itemsByKey.Add(
                                    key:    addition.Key,
                                    value:  addition.Item);
                                break;

                            case KeyedChangeType.Removal:
                                _itemsByKey.Remove(change.AsRemoval().Key);
                                break;

                            case KeyedChangeType.Replacement:
                                var replacement = change.AsReplacement();
                                _itemsByKey[replacement.Key] = replacement.NewItem;
                                break;

                            default:
                                throw new ArgumentException($"Unsupported {nameof(KeyedChange)} type {change.Type} in {nameof(IKeyedChangeSet<TKey, TValue>)}", nameof(changeSet));
                        }
                    }
                    break;

                default:
                    throw new ArgumentException($"Unsupported {nameof(IKeyedChangeSet<TKey, TValue>)} type {changeSet.Type}", nameof(changeSet));
            }

            _changeSets.OnNext(changeSet);
            _collectionChanged.OnNext(Unit.Default);
        }

        public void Clear()
        {
            if (_itemsByKey.Count is 0)
                return;

            var changeSet = null as IKeyedChangeSet<TKey, TValue>;
            if (_changeSets.HasObservers)
            {
                var removals = ImmutableArray.CreateBuilder<KeyedRemoval<TKey, TValue>>(initialCapacity: _itemsByKey.Count);
                foreach(var pair in _itemsByKey)
                    removals.Add(new()
                    {
                        Item    = pair.Value,
                        Key     = pair.Key
                    });

                changeSet = new KeyedClearChangeSet<TKey, TValue>()
                {
                    Removals = removals.MoveToImmutable()
                };
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
                        switch(changeSet.Type)
                        {
                            case ChangeSetType.Clear:
                            case ChangeSetType.Reset:
                                observer.OnCompleted();
                                break;

                            case ChangeSetType.Update:
                                foreach(var change in changeSet)
                                    switch(change.Type)
                                    {
                                        case KeyedChangeType.Addition:
                                            if (EqualityComparer<TKey>.Default.Equals(change.AsAddition().Key, key))
                                                throw new InvalidOperationException($"Unable to process {nameof(KeyedChange)} type {nameof(KeyedChangeType.Addition)}: Key is already present in the collection");
                                            break;

                                        case KeyedChangeType.Removal:
                                            if (EqualityComparer<TKey>.Default.Equals(change.AsRemoval().Key, key))
                                            {
                                                observer.OnCompleted();
                                                goto END_UPDATE;
                                            }
                                            break;

                                        case KeyedChangeType.Replacement:
                                            var replacement = change.AsReplacement();
                                            if (EqualityComparer<TKey>.Default.Equals(replacement.Key, key))
                                            {
                                                observer.OnNext(replacement.NewItem);
                                                goto END_UPDATE;
                                            }
                                            break;

                                        default:
                                            throw new ArgumentException($"Unsupported {nameof(KeyedChange)} type {change.Type} in {nameof(IKeyedChangeSet<TKey, TValue>)}", nameof(changeSet));
                                    }
                                END_UPDATE:
                                break;

                            default:
                                throw new ArgumentException($"Unsupported {nameof(IKeyedChangeSet<TKey, TValue>)} type {changeSet.Type}", nameof(changeSet));
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
                _changeSets.OnNext(new KeyedRemovalChangeSet<TKey, TValue>()
                {
                    Item    = value,
                    Key     = key
                });
            _collectionChanged.OnNext(Unit.Default);

            return true;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (!((ICollection<KeyValuePair<TKey, TValue>>)_itemsByKey).Remove(item))
                return false;

            if (_changeSets.HasObservers)
                _changeSets.OnNext(new KeyedRemovalChangeSet<TKey, TValue>()
                {
                    Item    = item.Value,
                    Key     = item.Key
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
                var removals = ImmutableArray.CreateBuilder<KeyedRemoval<TKey, TValue>>(initialCapacity: _itemsByKey.Count);
                foreach(var pair in _itemsByKey)
                    removals.Add(new()
                    {
                        Item    = pair.Value,
                        Key     = pair.Key
                    });

                _itemsByKey.Clear();

                var additions = ImmutableArray.CreateBuilder<KeyedAddition<TKey, TValue>>(initialCapacity: valuesCount);

                foreach(var newValue in values)
                {
                    var key = keySelector.Invoke(newValue);
                    _itemsByKey.Add(key, newValue);
                    additions.Add(new()
                    {
                        Item    = newValue,
                        Key     = key
                    });
                }

                _changeSets.OnNext(new KeyedResetChangeSet<TKey, TValue>()
                {
                    Additions   = additions.MoveToOrCreateImmutable(),
                    Removals    = removals.MoveToImmutable()
                });
            }
            else
            {
                _itemsByKey.Clear();
                foreach(var value in values)
                    _itemsByKey.Add(keySelector.Invoke(value), value);
            }

            _collectionChanged.OnNext(Unit.Default);
        }

        public IDisposable Subscribe(IObserver<IKeyedChangeSet<TKey, TValue>> observer)
        {
            if (_itemsByKey.Count is not 0)
            {
                var additions = ImmutableArray.CreateBuilder<KeyedAddition<TKey, TValue>>(initialCapacity: _itemsByKey.Count);

                foreach(var pair in _itemsByKey)
                    additions.Add(new()
                    {
                        Item    = pair.Value,
                        Key     = pair.Key
                    });

                observer.OnNext(new KeyedRangeAdditionChangeSet<TKey, TValue>()
                {
                    Additions = additions.MoveToImmutable()
                });
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
                var additions = ImmutableArray.CreateBuilder<KeyedAddition<TKey, TValue>>(initialCapacity: valuesCount);

                foreach(var value in values)
                {
                    var key = keySelector.Invoke(value);
                    _itemsByKey.Add(key, value);
                    additions.Add(new()
                    {
                        Item    = value,
                        Key     = key
                    });
                }

                _changeSets.OnNext(new KeyedRangeAdditionChangeSet<TKey, TValue>()
                {
                    Additions = additions.MoveToOrCreateImmutable()
                });
            }
            else
                foreach(var value in values)
                    _itemsByKey.Add(keySelector.Invoke(value), value);

            _collectionChanged.OnNext(Unit.Default);
        }

        private readonly Subject<IKeyedChangeSet<TKey, TValue>> _changeSets;
        private readonly Subject<Unit>                          _collectionChanged;
        private readonly Dictionary<TKey, TValue>               _itemsByKey;
    }
}
