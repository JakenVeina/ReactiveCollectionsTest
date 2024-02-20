using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive.Linq;

namespace ReactiveCollectionsTest.ImmutableArrayChanges
{
    public static class ObservableExtensions
    {
        public static IObservable<SortedChangeSet<T>> DisposeItemsAfterRemoval<T>(this IObservable<SortedChangeSet<T>> source)
                where T : IDisposable
            => source.DoAfter(changeSet =>
            {
                foreach(var change in changeSet.Changes)
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
                                var insertion = change.AsInsertion();
                                var selectedItem = itemSelector.Invoke(insertion.Item);
                                selectedItemsCache.Insert(
                                    index:  insertion.Index,
                                    item:   selectedItem);
                                changes.Add(SortedChange.Insertion(
                                    index:  insertion.Index,
                                    item:   selectedItem));
                            }
                            break;

                        case SortedChangeType.Movement:
                            {
                                var movement = change.AsMovement();
                                var selectedItem = selectedItemsCache[movement.OldIndex];
                                selectedItemsCache.ShuffleMove(
                                    oldIndex:   movement.OldIndex,
                                    newIndex:   movement.NewIndex);
                                changes.Add(SortedChange.Movement(
                                    oldIndex:   movement.OldIndex,
                                    newIndex:   movement.NewIndex,
                                    item:       selectedItem));
                            }
                            break;

                        case SortedChangeType.Removal:
                            {
                                var removal = change.AsRemoval();
                                var selectedItem = selectedItemsCache[removal.Index];
                                selectedItemsCache.RemoveAt(removal.Index);
                                changes.Add(SortedChange.Removal(
                                    index:  removal.Index,
                                    item:   selectedItem));
                            }
                            break;

                        case SortedChangeType.Replacement:
                            {
                                var replacement = change.AsReplacement();
                                var oldSelectedItem = selectedItemsCache[replacement.Index];
                                var newSelectedItem = itemSelector.Invoke(replacement.NewItem);

                                if (!EqualityComparer<TOut>.Default.Equals(oldSelectedItem, newSelectedItem))
                                {
                                    selectedItemsCache[replacement.Index] = newSelectedItem;
                                    changes.Add(SortedChange.Replacement(
                                        index:      replacement.Index,
                                        oldItem:    oldSelectedItem,
                                        newItem:    newSelectedItem));
                                }
                            }
                            break;

                        case SortedChangeType.Update:
                            {
                                var update = change.AsUpdate();
                                var oldSelectedItem = selectedItemsCache[update.OldIndex];
                                var newSelectedItem = itemSelector.Invoke(update.NewItem);

                                selectedItemsCache.ShuffleMove(
                                    oldIndex:   update.OldIndex,
                                    newIndex:   update.NewIndex);

                                if (EqualityComparer<TOut>.Default.Equals(oldSelectedItem, newSelectedItem))
                                    changes.Add(SortedChange.Movement(
                                        oldIndex:   update.OldIndex,
                                        newIndex:   update.NewIndex,
                                        item:       oldSelectedItem));
                                else
                                {
                                    selectedItemsCache[update.NewIndex] = newSelectedItem;
                                    changes.Add(SortedChange.Update(
                                        oldIndex:   update.OldIndex,
                                        oldItem:    oldSelectedItem,
                                        newIndex:   update.NewIndex,
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
                                                var addition = change.AsAddition();
                                                newSortedItems.Add(addition.Item);
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
                                                    var addition = change.AsAddition();
                                                    var insertionIndex = sortedItems.FindSortingIndex(addition.Item, itemComparer);

                                                    sortedItems.Insert(insertionIndex, addition.Item);

                                                    changes.Add(SortedChange.Insertion(
                                                        index:  insertionIndex,
                                                        item:   addition.Item));
                                                }
                                                break;
                                            
                                            case KeyedChangeType.Removal:
                                                {
                                                    var removal = change.AsRemoval();
                                                    var removalIndex = sortedItems.FindSortingIndex(removal.Item, itemComparer);

                                                    sortedItems.RemoveAt(removalIndex);

                                                    changes.Add(SortedChange.Removal(
                                                        index:  removalIndex,
                                                        item:   removal.Item));
                                                }
                                                break;

                                            case KeyedChangeType.Replacement:
                                                {
                                                    var replacement = change.AsReplacement();
                                                    var removalIndex = sortedItems.FindSortingIndex(replacement.OldItem, itemComparer);
                                                    var insertionIndex = sortedItems.FindSortingIndex(replacement.NewItem, itemComparer);

                                                    // We'll perform removal first, which will offset the insertion index, if it's ahead of the removal
                                                    if (insertionIndex > removalIndex)
                                                        --insertionIndex;

                                                    if (removalIndex == insertionIndex)
                                                    {
                                                        sortedItems[insertionIndex] = replacement.NewItem;

                                                        changes.Add(SortedChange.Replacement(
                                                            index:      removalIndex,
                                                            oldItem:    replacement.OldItem,
                                                            newItem:    replacement.NewItem));
                                                    }
                                                    else
                                                    {
                                                        sortedItems.RemoveAt(removalIndex);
                                                        sortedItems.Insert(insertionIndex, replacement.NewItem);

                                                        changes.Add(SortedChange.Update(
                                                            oldIndex:   removalIndex,
                                                            oldItem:    replacement.OldItem,
                                                            newIndex:   insertionIndex,
                                                            newItem:    replacement.NewItem));
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
                                if (predicate.Invoke(change.AsRemoval().Item))
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
                                        var addition = change.AsAddition();
                                        if (predicate.Invoke(addition.Item))
                                        {
                                            changes.Add(change);
                                            changesHasAdditions = true;
                                        }
                                        break;

                                    case KeyedChangeType.Removal:
                                        var removal = change.AsRemoval();
                                        if (predicate.Invoke(removal.Item))
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
                                        var addition = change.AsAddition();
                                        if (predicate.Invoke(addition.Item))
                                            changes.Add(change);
                                        break;

                                    case KeyedChangeType.Removal:
                                        var removal = change.AsRemoval();
                                        if (predicate.Invoke(removal.Item))
                                            changes.Add(change);
                                        break;

                                    case KeyedChangeType.Replacement:
                                        var replacement = change.AsReplacement();
                                        switch((predicate.Invoke(replacement.OldItem), predicate.Invoke(replacement.NewItem)))
                                        {
                                            case (true, true):
                                                changes.Add(change);
                                                break;

                                            case (true, false):
                                                changes.Add(KeyedChange.Removal(
                                                    key:    replacement.Key,
                                                    item:   replacement.OldItem));
                                                break;

                                            case (false, true):
                                                changes.Add(KeyedChange.Addition(
                                                    key:    replacement.Key,
                                                    item:   replacement.NewItem));
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
