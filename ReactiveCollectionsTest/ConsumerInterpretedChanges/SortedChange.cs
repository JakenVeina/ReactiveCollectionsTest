using System;
using System.Collections.Generic;

namespace ReactiveCollectionsTest.ConsumerInterpretedChanges
{
    public static class SortedChange
    {
        public static SortedChange<T> Insertion<T>(
                int index,
                T   item)
            => SortedChange<T>.Insertion(
                index:  index,
                item:   item);

        public static SortedChange<T> Movement<T>(
                int oldIndex,
                int newIndex,
                T   item)
            => SortedChange<T>.Movement(
                oldIndex:   oldIndex,
                newIndex:   newIndex,
                item:       item);

        public static SortedChange<T> Removal<T>(
                int index,
                T   item)
            => SortedChange<T>.Removal(
                index:  index,
                item:   item);

        public static SortedChange<T> Replacement<T>(
                int index,
                T   oldItem,
                T   newItem)
            => SortedChange<T>.Replacement(
                index:      index,
                newItem:    newItem,
                oldItem:    oldItem);

        public static SortedChange<T> Update<T>(
                int oldIndex,
                T   oldItem,
                int newIndex,
                T   newItem)
            => SortedChange<T>.Update(
                oldIndex:   oldIndex,
                oldItem:    oldItem,
                newIndex:   newIndex,
                newItem:    newItem);
    }

    public readonly record struct SortedChange<T>
    {
        public static SortedChange<T> Insertion(
                int index,
                T   item)
            => new()
            {
                NewIndex    = index,
                NewItem     = item,
                Type        = SortedChangeType.Insertion
            };

        public static SortedChange<T> Movement(
                int oldIndex,
                int newIndex,
                T   item)
            => new()
            {
                NewIndex    = newIndex,
                NewItem     = item,
                OldIndex    = oldIndex,
                Type        = SortedChangeType.Movement
            };

        public static SortedChange<T> Removal(
                int index,
                T   item)
            => new()
            {
                OldIndex    = index,
                OldItem     = item,
                Type        = SortedChangeType.Removal
            };

        public static SortedChange<T> Replacement(
                int index,
                T   oldItem,
                T   newItem)
            => new()
            {
                OldIndex    = index,
                OldItem     = oldItem,
                NewIndex    = index,
                NewItem     = newItem,
                Type        = SortedChangeType.Replacement
            };

        public static SortedChange<T> Update(
                int oldIndex,
                T   oldItem,
                int newIndex,
                T   newItem)
            => new()
            {
                OldIndex    = oldIndex,
                OldItem     = oldItem,
                NewIndex    = newIndex,
                NewItem     = newItem,
                Type        = SortedChangeType.Update
            };

        public Optional<int> NewIndex { get; private init; }
        
        public Optional<T> NewItem { get; private init; }
        
        public Optional<int> OldIndex { get; private init; }
        
        public Optional<T> OldItem { get; private init; }

        public SortedChangeType Type { get; private init; }

        public SortedChange<U>? WithSelectedItems<U>(Func<T, U> itemSelector)
        {
            var newItem = NewItem.IsSpecified
                ? itemSelector.Invoke(NewItem.Value)
                : Optional<U>.Unspecified;

            var oldItem = OldItem.IsSpecified
                ? itemSelector.Invoke(OldItem.Value)
                : Optional<U>.Unspecified;

            return ((Type is SortedChangeType.Replacement) && EqualityComparer<U>.Default.Equals(newItem.Value, oldItem.Value))
                ? null
                : new()
                {
                    NewIndex    = NewIndex,
                    NewItem     = newItem,
                    OldIndex    = OldIndex,
                    OldItem     = oldItem,
                    Type        = ((Type is SortedChangeType.Update) && EqualityComparer<U>.Default.Equals(newItem.Value, oldItem.Value))
                        ? SortedChangeType.Movement
                        : Type
                };
        }
    }
}
