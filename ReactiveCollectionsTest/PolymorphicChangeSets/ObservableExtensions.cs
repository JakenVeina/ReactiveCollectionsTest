using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;

namespace ReactiveCollectionsTest.PolymorphicChangeSets
{
    public static class ObservableExtensions
    {
        public static IObservable<ISortedChangeSet<T>> DisposeItemsAfterRemoval<T>(this IObservable<ISortedChangeSet<T>> source)
                where T : IDisposable
            => source.DoAfter(changeSet =>
            {
                foreach(var change in changeSet)
                    switch(change.Type)
                    {
                        case SortedChangeType.Removal:
                            change.AsRemoval().Item.Dispose();
                            break;

                        case SortedChangeType.Replacement:
                            change.AsReplacement().OldItem.Dispose();
                            break;

                        case SortedChangeType.Update:
                            change.AsUpdate().OldItem.Dispose();
                            break;
                    }
            });

        public static IObservable<ISortedChangeSet<TItem>> OrderItems<TKey, TItem>(
                this    IObservable<IKeyedChangeSet<TKey, TItem>>   source,
                        IComparer<TItem>                            itemComparer)
            => Observable.Create<ISortedChangeSet<TItem>>(observer =>
            {
                var sortedItems = new List<TItem>();

                return source
                    .Select<IKeyedChangeSet<TKey, TItem>, ISortedChangeSet<TItem>>(changeSet =>
                    {
                        switch(changeSet)
                        {
                            case KeyedAdditionChangeSet<TKey, TItem> addition:
                                {
                                    var index = sortedItems.FindSortingIndex(addition.Item, itemComparer);
                                    sortedItems.Insert(index, addition.Item);

                                    return new SortedInsertionChangeSet<TItem>()
                                    {
                                        Index   = index,
                                        Item    = addition.Item
                                    };
                                }

                            case KeyedClearChangeSet<TKey, TItem>:
                                var items = sortedItems.ToImmutableArray();
                                sortedItems.Clear();

                                return new SortedClearChangeSet<TItem>()
                                {
                                    Items = items
                                };

                            case KeyedRangeAdditionChangeSet<TKey, TItem> rangeAddition:
                                {
                                    var insertions = ImmutableArray.CreateBuilder<SortedInsertion<TItem>>(initialCapacity: rangeAddition.Additions.Length);
                                    foreach (var addition in rangeAddition.Additions)
                                    {
                                        var index = sortedItems.FindSortingIndex(addition.Item, itemComparer);
                                        sortedItems.Insert(index, addition.Item);

                                        insertions.Add(new()
                                        {
                                            Index   = index,
                                            Item    = addition.Item
                                        });
                                    }

                                    return new SortedRangeAdditionChangeSet<TItem>()
                                    {
                                        Insertions = insertions.MoveToImmutable()
                                    };
                                }

                            case KeyedRemovalChangeSet<TKey, TItem> removal:
                                {
                                    var index = sortedItems.FindSortingIndex(removal.Item, itemComparer);
                                    sortedItems.RemoveAt(index);

                                    return new SortedRemovalChangeSet<TItem>()
                                    {
                                        Index   = index,
                                        Item    = removal.Item,
                                    };
                                }

                            case KeyedReplacementChangeSet<TKey, TItem> replacement:
                                {
                                    var removalIndex = sortedItems.FindSortingIndex(replacement.OldItem, itemComparer);
                                    var insertionIndex = sortedItems.FindSortingIndex(replacement.NewItem, itemComparer);

                                    // We'll perform removal first, which will offset the insertion index, if it's ahead of the removal
                                    if (insertionIndex > removalIndex)
                                        --insertionIndex;

                                    if (removalIndex == insertionIndex)
                                    {
                                        sortedItems[insertionIndex] = replacement.NewItem;

                                        return new SortedReplacementChangeSet<TItem>()
                                        {
                                            Index   = removalIndex,
                                            NewItem = replacement.NewItem,
                                            OldItem = replacement.OldItem
                                        };
                                    }
                                    else
                                    {
                                        sortedItems.RemoveAt(removalIndex);
                                        sortedItems.Insert(insertionIndex, replacement.NewItem);

                                        return new SortedUpdateChangeSet<TItem>()
                                        {
                                            NewIndex    = removalIndex,
                                            NewItem     = replacement.NewItem,
                                            OldIndex    = insertionIndex,
                                            OldItem     = replacement.OldItem
                                        };
                                    }
                                }

                            case KeyedResetChangeSet<TKey, TItem> reset:
                                var oldItems = sortedItems.ToImmutableArray();
                                sortedItems.Clear();

                                foreach (var additions in reset.Additions)
                                    sortedItems.Add(additions.Item);
                                sortedItems.Sort(itemComparer);

                                return new SortedResetChangeSet<TItem>()
                                {
                                    NewItems = sortedItems.ToImmutableArray(),
                                    OldItems = oldItems
                                };

                            default:
                                throw new ArgumentException($"Unsupported {nameof(IKeyedChangeSet<TKey, TItem>)} type {changeSet.GetType()}");
                        }
                    })
                    .Subscribe(observer);
            });

        public static IObservable<ISortedChangeSet<TOut>> SelectAndCacheItems<TIn, TOut>(
            this    IObservable<ISortedChangeSet<TIn>>  source,
                    Func<TIn, TOut>                     itemSelector)
        {
            var selectedItemsCache = new List<TOut>();

            return source.Select<ISortedChangeSet<TIn>, ISortedChangeSet<TOut>>(changeSet =>
            {
                switch (changeSet)
                {
                    case SortedClearChangeSet<TIn>:
                        var items = selectedItemsCache.ToImmutableArray();
                        selectedItemsCache.Clear();

                        return new SortedClearChangeSet<TOut>()
                        {
                            Items = items
                        };

                    case SortedInsertionChangeSet<TIn> insertion:
                        {
                            var selectedItem = itemSelector.Invoke(insertion.Item);
                            selectedItemsCache.Insert(insertion.Index, selectedItem);
                        
                            return new SortedInsertionChangeSet<TOut>()
                            {
                                Index   = insertion.Index,
                                Item    = selectedItem
                            };
                        }

                    case SortedMovementChangeSet<TIn> movement:
                        {
                            var selectedItem = selectedItemsCache[movement.OldIndex];
                            selectedItemsCache.ShuffleMove(movement.OldIndex, movement.NewIndex);

                            return new SortedMovementChangeSet<TOut>()
                            {
                                Item        = selectedItem,
                                NewIndex    = movement.OldIndex,
                                OldIndex    = movement.NewIndex,
                            };
                        }

                    case SortedRangeAdditionChangeSet<TIn> rangeAddition:
                        var insertions = ImmutableArray.CreateBuilder<SortedInsertion<TOut>>(initialCapacity: rangeAddition.Insertions.Length);
                        foreach (var insertion in rangeAddition.Insertions)
                        {
                            var selectedItem = itemSelector.Invoke(insertion.Item);
                            selectedItemsCache.Insert(insertion.Index, selectedItem);

                            insertions.Add(new()
                            {
                                Index   = insertion.Index,
                                Item    = selectedItem
                            });
                        }

                        return new SortedRangeAdditionChangeSet<TOut>()
                        {
                            Insertions = insertions.MoveToImmutable()
                        };

                    case SortedRemovalChangeSet<TIn> removal:
                        {
                            var selectedItem = selectedItemsCache[removal.Index];
                            selectedItemsCache.RemoveAt(removal.Index);

                            return new SortedRemovalChangeSet<TOut>()
                            {
                                Index   = removal.Index,
                                Item    = selectedItem
                            };
                        }

                    case SortedReplacementChangeSet<TIn> replacement:
                        {
                            var oldSelectedItem = selectedItemsCache[replacement.Index];
                            var newSelectedItem = itemSelector.Invoke(replacement.OldItem);
                            selectedItemsCache[replacement.Index] = newSelectedItem;

                            return new SortedReplacementChangeSet<TOut>()
                            {
                                Index   = replacement.Index,
                                NewItem = newSelectedItem,
                                OldItem = oldSelectedItem
                            };
                        }

                    case SortedResetChangeSet<TIn> reset:
                        var oldItems = selectedItemsCache.ToImmutableArray();
                        selectedItemsCache.Clear();

                        selectedItemsCache.EnsureCapacity(reset.NewItems.Length);
                        var newItems = ImmutableArray.CreateBuilder<TOut>(initialCapacity: reset.NewItems.Length);
                        foreach (var newItem in reset.NewItems)
                        {
                            var selectedItem = itemSelector.Invoke(newItem);
                            selectedItemsCache.Add(selectedItem);

                            newItems.Add(selectedItem);
                        }

                        return new SortedResetChangeSet<TOut>()
                        {
                            NewItems = newItems.MoveToImmutable(),
                            OldItems = oldItems
                        };

                    case SortedUpdateChangeSet<TIn> update:
                        {
                            var oldSelectedItem = selectedItemsCache[update.OldIndex];
                            var newSelectedItem = itemSelector.Invoke(update.NewItem);
                            selectedItemsCache.ShuffleMove(update.OldIndex, update.NewIndex);
                            selectedItemsCache[update.NewIndex] = newSelectedItem;

                            return new SortedUpdateChangeSet<TOut>()
                            {
                                NewIndex    = update.NewIndex,
                                NewItem     = newSelectedItem,
                                OldIndex    = update.OldIndex,
                                OldItem     = oldSelectedItem
                            };
                        }

                    default:
                        throw new ArgumentException($"Unsupported {nameof(ISortedChangeSet<TIn>)} type {changeSet.GetType()}");
                }
            });
        }

        public static IObservable<ISortedChangeSet<TOut>> SelectItems<TIn, TOut>(
                this    IObservable<ISortedChangeSet<TIn>>  source,
                        Func<TIn, TOut>                     itemSelector)
            => source.SelectSome<ISortedChangeSet<TIn>, ISortedChangeSet<TOut>>((changeSet, onNext) =>
            {
                var newChangeSet = changeSet.Transform(itemSelector);
                if (newChangeSet is not null)
                    onNext.Invoke(newChangeSet);
            });

        public static IObservable<IKeyedChangeSet<TKey, TOut>> SelectItemValues<TKey, TIn, TOut>(
                this    IObservable<IKeyedChangeSet<TKey, TIn>> source,
                        Func<TIn, TOut>                         itemValueSelector)
            => source.SelectSome<IKeyedChangeSet<TKey, TIn>, IKeyedChangeSet<TKey, TOut>>((changeSet, onNext) =>
            {
                var newChangeSet = changeSet.Transform(itemValueSelector);
                if (newChangeSet is not null)
                    onNext.Invoke(newChangeSet);
            });

        public static IObservable<IKeyedChangeSet<TKey, TValue>> WhereItems<TKey, TValue>(
                    this    IObservable<IKeyedChangeSet<TKey, TValue>>  source,
                            Predicate<TValue>                           predicate)
                where TKey : notnull
            => source.SelectSome<IKeyedChangeSet<TKey, TValue>, IKeyedChangeSet<TKey, TValue>>((changeSet, onNext) =>
            {
                var newChangeSet = changeSet.Filter(predicate);
                if (newChangeSet is not null)
                    onNext.Invoke(newChangeSet);
            });
    }
}
