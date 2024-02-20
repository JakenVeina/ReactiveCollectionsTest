using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive.Linq;

using Microsoft.Diagnostics.Tracing.Parsers.MicrosoftWindowsTCPIP;

namespace ReactiveCollectionsTest.ConsumerInterpretedChanges
{
    public static class ObservableExtensions
    {
        public static IObservable<SortedChangeSet<T>> DisposeItemsAfterRemoval<T>(this IObservable<SortedChangeSet<T>> source)
                where T : IDisposable
            => source.DoAfter(changeSet =>
            {
                foreach(var change in changeSet.Changes)
                    if (change.OldItem.IsSpecified)
                        change.OldItem.Value.Dispose();
            });

        public static IObservable<SortedChangeSet<TItem>> OrderItems<TKey, TItem>(
                this    IObservable<KeyedChangeSet<TKey, TItem>>    source,
                        IComparer<TItem>                            itemComparer)
            => Observable.Create<SortedChangeSet<TItem>>(observer =>
            {
                var sortedItems = new List<TItem>();

                return source
                    .Select(changeSet =>
                    {
                        switch(changeSet.Type)
                        {
                            case ChangeSetType.Clear:
                                {
                                    var destinationChangeSet = SortedChangeSet.Clear(sortedItems);
                                    sortedItems.Clear();
                                    return destinationChangeSet;
                                }

                            case ChangeSetType.Reset:
                                {
                                    var newSortedItems = new List<TItem>(capacity: changeSet.Changes.Length - sortedItems.Count);

                                    foreach(var change in changeSet.Changes)
                                    {
                                        switch(change.Type)
                                        {
                                            case KeyedChangeType.Addition:
                                                newSortedItems.Add(change.NewItem.Value);
                                                break;

                                            case KeyedChangeType.Removal:
                                                break;

                                            default:
                                                throw new InvalidOperationException($"Unsupported {nameof(KeyedChange)} type {change.Type} within {nameof(KeyedChangeSet)} of type {changeSet.Type}");
                                        }
                                    }

                                    newSortedItems.Sort(itemComparer);
                                    var oldSortedItems = sortedItems;
                                    sortedItems = newSortedItems;

                                    return SortedChangeSet.Reset(
                                        oldSortedItems: oldSortedItems,
                                        newSortedItems: newSortedItems);
                                }

                            case ChangeSetType.Update:
                                {
                                    var changes = ImmutableArray.CreateBuilder<SortedChange<TItem>>(initialCapacity: changeSet.Changes.Length);

                                    foreach(var change in changeSet.Changes)
                                    {
                                        switch(change.Type)
                                        {
                                            case KeyedChangeType.Addition:
                                                {
                                                    var insertionIndex = sortedItems.FindSortingIndex(change.NewItem.Value, itemComparer);

                                                    sortedItems.Insert(insertionIndex, change.NewItem.Value);

                                                    changes.Add(SortedChange.Insertion(
                                                        index:  insertionIndex,
                                                        item:   change.NewItem.Value));
                                                }
                                                break;
                                            
                                            case KeyedChangeType.Removal:
                                                {
                                                    var removalIndex = sortedItems.FindSortingIndex(change.OldItem.Value, itemComparer);

                                                    sortedItems.RemoveAt(removalIndex);

                                                    changes.Add(SortedChange.Removal(
                                                        index:  removalIndex,
                                                        item:   change.OldItem.Value));
                                                }
                                                break;

                                            case KeyedChangeType.Replacement:
                                                {
                                                    var oldIndex = sortedItems.FindSortingIndex(change.OldItem.Value, itemComparer);
                                                    var newIndex = sortedItems.FindSortingIndex(change.NewItem.Value, itemComparer);

                                                    // We'll perform removal first, which will offset the insertion index, if it's ahead of the removal
                                                    if (newIndex > oldIndex)
                                                        --newIndex;

                                                    if (oldIndex == newIndex)
                                                    {
                                                        sortedItems[newIndex] = change.NewItem.Value;

                                                        changes.Add(SortedChange.Replacement(
                                                            index:      oldIndex,
                                                            oldItem:    change.OldItem.Value,
                                                            newItem:    change.NewItem.Value));
                                                    }
                                                    else
                                                    {
                                                        sortedItems.RemoveAt(oldIndex);
                                                        sortedItems.Insert(newIndex, change.NewItem.Value);

                                                        changes.Add(SortedChange.Update(
                                                            oldIndex:   oldIndex,
                                                            oldItem:    change.OldItem.Value,
                                                            newIndex:   newIndex,
                                                            newItem:    change.NewItem.Value));
                                                    }
                                                }
                                                break;

                                            default:
                                                throw new InvalidOperationException($"Unsupported {nameof(KeyedChange)} type {change.Type}");
                                        }
                                    }

                                    return new SortedChangeSet<TItem>()
                                    {
                                        Changes = changes.MoveToImmutable(),
                                        Type    = ChangeSetType.Update
                                    };
                                }

                            default:
                                throw new InvalidOperationException($"Unsupported {nameof(KeyedChangeSet)} type {changeSet.Type}");
                        }
                    })
                    .Subscribe(observer);
            });

        public static IObservable<SortedChangeSet<TOut>> SelectAndCacheItems<TIn, TOut>(
            this    IObservable<SortedChangeSet<TIn>>   source,
                    Func<TIn, TOut>                     itemSelector)
        {
            var selectedItemsCache = new List<TOut>();

            return source.SelectSome<SortedChangeSet<TIn>, SortedChangeSet<TOut>>((changeSet, onNext) =>
            {
                var changes = ImmutableArray.CreateBuilder<SortedChange<TOut>>(initialCapacity: changeSet.Changes.Length);

                foreach(var change in changeSet.Changes)
                {
                    switch(change.Type)
                    {
                        case SortedChangeType.Insertion:
                            {
                                var selectedItem = itemSelector.Invoke(change.NewItem.Value);
                                selectedItemsCache.Insert(
                                    index:  change.NewIndex.Value,
                                    item:   selectedItem);
                                changes.Add(SortedChange.Insertion(
                                    index:  change.NewIndex.Value,
                                    item:   selectedItem));
                            }
                            break;

                        case SortedChangeType.Movement:
                            {
                                var selectedItem = selectedItemsCache[change.OldIndex.Value];
                                selectedItemsCache.ShuffleMove(
                                    oldIndex:   change.OldIndex.Value,
                                    newIndex:   change.NewIndex.Value);
                                changes.Add(SortedChange.Movement(
                                    oldIndex:   change.OldIndex.Value,
                                    newIndex:   change.NewIndex.Value,
                                    item:       selectedItem));
                            }
                            break;

                        case SortedChangeType.Removal:
                            {
                                var selectedItem = selectedItemsCache[change.OldIndex.Value];
                                selectedItemsCache.RemoveAt(change.OldIndex.Value);
                                changes.Add(SortedChange.Removal(
                                    index:  change.OldIndex.Value,
                                    item:   selectedItem));
                            }
                            break;

                        case SortedChangeType.Replacement:
                            {
                                var oldSelectedItem = selectedItemsCache[change.OldIndex.Value];
                                var newSelectedItem = itemSelector.Invoke(change.NewItem.Value);

                                if (!EqualityComparer<TOut>.Default.Equals(oldSelectedItem, newSelectedItem))
                                {
                                    selectedItemsCache[change.NewIndex.Value] = newSelectedItem;
                                    changes.Add(SortedChange.Replacement(
                                        index:      change.NewIndex.Value,
                                        oldItem:    oldSelectedItem,
                                        newItem:    newSelectedItem));
                                }
                            }
                            break;

                        case SortedChangeType.Update:
                            {
                                var oldSelectedItem = selectedItemsCache[change.OldIndex.Value];
                                var newSelectedItem = itemSelector.Invoke(change.NewItem.Value);

                                selectedItemsCache.ShuffleMove(
                                    oldIndex:   change.OldIndex.Value,
                                    newIndex:   change.NewIndex.Value);

                                if (EqualityComparer<TOut>.Default.Equals(oldSelectedItem, newSelectedItem))
                                    changes.Add(SortedChange.Movement(
                                        oldIndex:   change.OldIndex.Value,
                                        newIndex:   change.NewIndex.Value,
                                        item:       oldSelectedItem));
                                else
                                {
                                    selectedItemsCache[change.NewIndex.Value] = newSelectedItem;
                                    changes.Add(SortedChange.Update(
                                        oldIndex:   change.OldIndex.Value,
                                        oldItem:    oldSelectedItem,
                                        newIndex:   change.NewIndex.Value,
                                        newItem:    newSelectedItem));
                                }
                            }
                            break;

                        default:
                            throw new InvalidOperationException($"Unsupported {nameof(SortedChange)} type {change.Type}");
                    }
                }

                if (changes.Count is not 0)
                    onNext.Invoke(new()
                    {
                        Changes = changes.MoveToOrCreateImmutable(),
                        Type    = changeSet.Type
                    });
            });
        }

        public static IObservable<SortedChangeSet<TOut>> SelectItems<TIn, TOut>(
                this    IObservable<SortedChangeSet<TIn>>   source,
                        Func<TIn, TOut>                     itemSelector)
            => source.SelectSome<SortedChangeSet<TIn>, SortedChangeSet<TOut>>((changeSet, onNext) =>
            {
                var newChangeSet = changeSet.WithSelectedItems(itemSelector);
                if (newChangeSet is not null)
                    onNext.Invoke(newChangeSet.Value);
            });

        public static IObservable<KeyedChangeSet<TKey, TOut>> SelectItemValues<TKey, TIn, TOut>(
                this    IObservable<KeyedChangeSet<TKey, TIn>>  source,
                        Func<TIn, TOut>                         itemValueSelector)
            => source.SelectSome<KeyedChangeSet<TKey, TIn>, KeyedChangeSet<TKey, TOut>>((changeSet, onNext) => 
            {
                var newChangeSet = changeSet.WithSelectedItems(itemValueSelector);
                if (newChangeSet is not null)
                    onNext.Invoke(newChangeSet.Value);
            });

        public static IObservable<KeyedChangeSet<TKey, TValue>> WhereItems<TKey, TValue>(
                    this    IObservable<KeyedChangeSet<TKey, TValue>>   source,
                            Predicate<TValue>                           predicate)
                where TKey : notnull
            => source.SelectSome<KeyedChangeSet<TKey, TValue>, KeyedChangeSet<TKey, TValue>>((changeSet, onNext) =>
            {
                switch(changeSet.Type)
                {
                    case ChangeSetType.Clear:
                        {
                            var changes = ImmutableArray.CreateBuilder<KeyedChange<TKey, TValue>>(initialCapacity: changeSet.Changes.Length);

                            foreach(var change in changeSet.Changes)
                                if (predicate.Invoke(change.OldItem.Value))
                                    changes.Add(change);

                            if (changes.Count is not 0)
                                onNext.Invoke(changeSet with
                                {
                                    Changes = changes.MoveToOrCreateImmutable()
                                });
                        }
                        break;

                    case ChangeSetType.Reset:
                        {
                            var changes = ImmutableArray.CreateBuilder<KeyedChange<TKey, TValue>>(initialCapacity: changeSet.Changes.Length);
                            var changesHasAdditions = false;
                            var changesHasRemovals = false;

                            foreach(var change in changeSet.Changes)
                                switch(change.Type)
                                {
                                    case KeyedChangeType.Addition:
                                        if (predicate.Invoke(change.NewItem.Value))
                                        {
                                            changes.Add(change);
                                            changesHasAdditions = true;
                                        }
                                        break;

                                    case KeyedChangeType.Removal:
                                        if (predicate.Invoke(change.OldItem.Value))
                                        {
                                            changes.Add(change);
                                            changesHasRemovals = true;
                                        }
                                        break;

                                    default:
                                        throw new InvalidOperationException($"Unsupported {nameof(KeyedChange)} type {change.Type} within {nameof(KeyedChangeSet)} of type {changeSet.Type}");
                                }

                            if (changes.Count is not 0)
                                onNext.Invoke(changeSet with
                                {
                                    Changes = changes.MoveToOrCreateImmutable(),
                                    Type    = (changesHasAdditions, changesHasRemovals) switch
                                    {
                                        (true, false)   => ChangeSetType.Update,
                                        (false, true)   => ChangeSetType.Clear,
                                        _               => ChangeSetType.Reset
                                    }
                                });
                        }
                        break;

                    case ChangeSetType.Update:
                        {
                            var changes = ImmutableArray.CreateBuilder<KeyedChange<TKey, TValue>>(initialCapacity: changeSet.Changes.Length);

                            foreach(var change in changeSet.Changes)
                                switch(change.Type)
                                {
                                    case KeyedChangeType.Addition:
                                        if (predicate.Invoke(change.NewItem.Value))
                                            changes.Add(change);
                                        break;

                                    case KeyedChangeType.Removal:
                                        if (predicate.Invoke(change.OldItem.Value))
                                            changes.Add(change);
                                        break;

                                    case KeyedChangeType.Replacement:
                                        switch((predicate.Invoke(change.OldItem.Value), predicate.Invoke(change.NewItem.Value)))
                                        {
                                            case (true, true):
                                                changes.Add(change);
                                                break;

                                            case (true, false):
                                                changes.Add(KeyedChange.Removal(
                                                    key:    change.Key,
                                                    item:   change.OldItem.Value));
                                                break;

                                            case (false, true):
                                                changes.Add(KeyedChange.Addition(
                                                    key:    change.Key,
                                                    item:   change.NewItem.Value));
                                                break;
                                        }
                                        break;

                                    default:
                                        throw new InvalidOperationException($"Unsupported {nameof(KeyedChange)} type {change.Type} within {nameof(KeyedChangeSet)} of type {changeSet.Type}");
                                }

                            if (changes.Count is not 0)
                                onNext.Invoke(changeSet with
                                {
                                    Changes = changes.MoveToOrCreateImmutable()
                                });
                        }
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported {nameof(KeyedChangeSet)} type {changeSet.Type}");
                }
            });
    }
}
