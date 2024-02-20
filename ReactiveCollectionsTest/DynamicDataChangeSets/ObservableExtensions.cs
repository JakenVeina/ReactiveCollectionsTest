using System;
using System.Collections.Generic;
using System.Reactive.Linq;

using DynamicData;

namespace ReactiveCollectionsTest.DynamicDataChangeSets
{
    public static class ObservableExtensions
    {
        public static IObservable<IChangeSet<T>> DisposeItemsAfterRemoval<T>(this IObservable<IChangeSet<T>> source)
                where T : IDisposable
            => source.DoAfter(changeSet =>
            {
                foreach(var change in changeSet)
                    if (change.Item.Previous.HasValue)
                        change.Item.Previous.Value.Dispose();
            });

        public static IObservable<IChangeSet<TItem>> OrderItems<TKey, TItem>(
                    this    IObservable<IChangeSet<TItem, TKey>>    source,
                            IComparer<TItem>                        itemComparer)
                where TKey : notnull
                where TItem : notnull
            => Observable.Create<IChangeSet<TItem>>(observer =>
            {
                var sortedItems = new List<TItem>();

                return source
                    .Select(changeSet =>
                    {
                        var changes = new ChangeSet<TItem>();

                        foreach(var change in changeSet)
                        {
                            switch(change.Reason)
                            {
                                case ChangeReason.Add:
                                    {
                                        var insertionIndex = sortedItems.FindSortingIndex(change.Current, itemComparer);

                                        sortedItems.Insert(insertionIndex, change.Current);

                                        changes.Add(new(
                                            reason:     ListChangeReason.Add,
                                            current:    change.Current,
                                            index:      insertionIndex));
                                    }
                                    break;
                                           
                                case ChangeReason.Remove:
                                    {
                                        var removalIndex = sortedItems.FindSortingIndex(change.Current, itemComparer);
                                        if (removalIndex >= sortedItems.Count)
                                            --removalIndex;

                                        sortedItems.RemoveAt(removalIndex);

                                        changes.Add(new(
                                            reason:     ListChangeReason.Remove,
                                            current:    change.Current,
                                            index:      removalIndex));
                                    }
                                    break;

                                case ChangeReason.Update:
                                    {
                                        var removalIndex = sortedItems.FindSortingIndex(change.Previous.Value, itemComparer);
                                        if (removalIndex >= sortedItems.Count)
                                            --removalIndex;

                                        var insertionIndex = sortedItems.FindSortingIndex(change.Current, itemComparer);
                                        // We'll perform removal first, which will offset the insertion index, if it's ahead of the removal
                                        if (insertionIndex > removalIndex)
                                            --insertionIndex;

                                        if (removalIndex == insertionIndex)
                                            sortedItems[insertionIndex] = change.Current;
                                        else
                                        {
                                            sortedItems.RemoveAt(removalIndex);
                                            sortedItems.Insert(insertionIndex, change.Current);
                                        }

                                        changes.Add(new(
                                            reason:         ListChangeReason.Replace,
                                            current:        change.Current,
                                            previous:       change.Previous,
                                            currentIndex:   insertionIndex,
                                            previousIndex:  removalIndex));
                                    }
                                    break;

                                default:
                                    throw new InvalidOperationException($"Unsupported {nameof(ChangeReason)} value {change.Reason}");
                            }
                        }

                        return changes;
                    })
                    .Subscribe(observer);
            });

        public static IObservable<IChangeSet<TOut>> SelectAndCacheItems<TIn, TOut>(
                this    IObservable<IChangeSet<TIn>>    source,
                        Func<TIn, TOut>                 itemSelector)
            where TIn : notnull
            where TOut : notnull
        {
            var selectedItemsCache = new List<TOut>();

            return source.SelectSome<IChangeSet<TIn>, IChangeSet<TOut>>((changeSet, onNext) =>
            {
                var changes = new ChangeSet<TOut>();

                foreach(var change in changeSet)
                {
                    switch(change.Reason)
                    {
                        case ListChangeReason.Add:
                            {
                                var selectedItem = itemSelector.Invoke(change.Item.Current);
                                selectedItemsCache.Insert(
                                    index:  change.Item.CurrentIndex,
                                    item:   selectedItem);
                                changes.Add(new(
                                    reason:     ListChangeReason.Add,
                                    current:    selectedItem,
                                    index:      change.Item.CurrentIndex));
                            }
                            break;

                        case ListChangeReason.Moved:
                            {
                                var selectedItem = selectedItemsCache[change.Item.PreviousIndex];
                                selectedItemsCache.ShuffleMove(
                                    oldIndex:   change.Item.PreviousIndex,
                                    newIndex:   change.Item.CurrentIndex);
                                changes.Add(new(
                                    reason:         ListChangeReason.Moved,
                                    current:        selectedItem,
                                    previous:       default,
                                    currentIndex:   change.Item.CurrentIndex,
                                    previousIndex:  change.Item.PreviousIndex));
                            }
                            break;

                        case ListChangeReason.Remove:
                            {
                                var selectedItem = selectedItemsCache[change.Item.CurrentIndex];
                                selectedItemsCache.RemoveAt(change.Item.CurrentIndex);
                                changes.Add(new(
                                    reason:     ListChangeReason.Remove,
                                    current:    selectedItem,
                                    index:      change.Item.CurrentIndex));
                            }
                            break;

                        case ListChangeReason.Replace:
                            {
                                var oldSelectedItem = selectedItemsCache[change.Item.PreviousIndex];
                                var newSelectedItem = itemSelector.Invoke(change.Item.Current);

                                if (EqualityComparer<TOut>.Default.Equals(oldSelectedItem, newSelectedItem))
                                {
                                    if (change.Item.PreviousIndex == change.Item.CurrentIndex)
                                        continue;

                                    selectedItemsCache.ShuffleMove(
                                        oldIndex:   change.Item.CurrentIndex,
                                        newIndex:   change.Item.PreviousIndex);
                                    
                                    changes.Add(new(
                                        reason:         ListChangeReason.Moved,
                                        current:        oldSelectedItem,
                                        previous:       default,
                                        currentIndex:   change.Item.CurrentIndex,
                                        previousIndex:  change.Item.PreviousIndex));
                                }
                                else
                                {
                                    if (change.Item.PreviousIndex != change.Item.CurrentIndex)
                                        selectedItemsCache.ShuffleMove(
                                            oldIndex:   change.Item.CurrentIndex,
                                            newIndex:   change.Item.PreviousIndex);

                                    selectedItemsCache[change.Item.CurrentIndex] = newSelectedItem;
                                    changes.Add(new(
                                        reason:         ListChangeReason.Moved,
                                        current:        newSelectedItem,
                                        previous:       oldSelectedItem,
                                        currentIndex:   change.Item.CurrentIndex,
                                        previousIndex:  change.Item.PreviousIndex));
                                }
                            }
                            break;

                        default:
                            throw new InvalidOperationException($"Unsupported {nameof(ListChangeReason)} value {change.Reason}");
                    }
                }

                if (changes.Count is not 0)
                    onNext.Invoke(changes);
            });
        }

        public static IObservable<IChangeSet<TOut>> SelectItems<TIn, TOut>(
                    this    IObservable<IChangeSet<TIn>>    source,
                            Func<TIn, TOut>                 itemSelector)
                where TIn : notnull
                where TOut : notnull
            => source.SelectSome<IChangeSet<TIn>, IChangeSet<TOut>>((changeSet, onNext) =>
            {
                var changes = new ChangeSet<TOut>();

                foreach(var change in changeSet)
                {
                    var newItem = itemSelector.Invoke(change.Item.Current);

                    var oldItem = change.Item.Previous.HasValue
                        ? itemSelector.Invoke(change.Item.Previous.Value)
                        : DynamicData.Kernel.Optional<TOut>.None;

                    if ((change.Reason is ListChangeReason.Replace) && EqualityComparer<TOut>.Default.Equals(newItem, oldItem.Value))
                        continue;

                    changes.Add(new(
                        reason:         change.Reason,
                        current:        newItem,
                        previous:       oldItem,
                        currentIndex:   change.Item.CurrentIndex,
                        previousIndex:  change.Item.PreviousIndex));
                }

                if (changes.Count is not 0)
                    onNext.Invoke(changes);
            });

        public static IObservable<IChangeSet<TItem, TKey>> WhereItems<TKey, TItem>(
                    this    IObservable<IChangeSet<TItem, TKey>>   source,
                            Predicate<TItem>                       predicate)
                where TKey : notnull
                where TItem : notnull
            => source.SelectSome<IChangeSet<TItem, TKey>, IChangeSet<TItem, TKey>>((changeSet, onNext) =>
            {
                var changes = new ChangeSet<TItem, TKey>(capacity: changeSet.Count);

                foreach (var change in (ChangeSet<TItem, TKey>)changeSet)
                {
                    // Propagate changes as follows:
                    //      Don't propagate moves at all, since we don't preserve indexes.
                    //      For Updates, propagate...
                    //          ...an Add if the new item matches the predicate, but the old one doesn't
                    //          ...a Remove if the old item matches the predicate, but the new one doesn't
                    //          ...an Update if both items match the predicate
                    //          ...nothing if neither items match the predicate
                    //      For all other changes, propagate only if the value matches the predicate
                    if (change.Reason is ChangeReason.Moved)
                        continue;

                    ChangeReason? downstreamReason = change.Reason switch
                    {
                        ChangeReason.Update => (predicate.Invoke(change.Previous.Value), predicate.Invoke(change.Current)) switch
                        {
                            (false, true) => ChangeReason.Add,
                            (true, false) => ChangeReason.Remove,
                            (true, true) => change.Reason,
                            _ => null
                        },
                        _ => predicate.Invoke(change.Current)
                            ? change.Reason
                            : null
                    };

                    if (downstreamReason is ChangeReason reason)
                    {
                        // Do not propagate indexes, we can't guarantee them to be correct, because we aren't caching items.
                        changes.Add(new(
                            reason:     reason,
                            key:        change.Key,
                            current:    change.Current,
                            previous:   (reason is ChangeReason.Update)
                                ? change.Previous
                                : default));
                    }
                }

                if (changes.Count is not 0)
                    onNext(changes);
            });
    }
}
