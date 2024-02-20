using System;
using System.Collections.Generic;

namespace ReactiveCollectionsTest.IReadOnlyListChanges
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

        public SortedChangeType Type { get; private init; }

        public SortedInsertion<T> AsInsertion()
            => (Type is SortedChangeType.Insertion)
                ? new()
                {
                    Index   = NewIndex,
                    Item    = NewItem
                }
                : throw new InvalidOperationException($"Cannot convert {nameof(SortedChange)} of type {Type} to {nameof(SortedInsertion<T>)}");

        public SortedMovement<T> AsMovement()
            => (Type is SortedChangeType.Movement)
                ? new()
                {
                    Item        = NewItem,
                    NewIndex    = NewIndex,
                    OldIndex    = OldIndex
                }
                : throw new InvalidOperationException($"Cannot convert {nameof(SortedChange)} of type {Type} to {nameof(SortedMovement<T>)}");

        public SortedRemoval<T> AsRemoval()
            => (Type is SortedChangeType.Removal)
                ? new()
                {
                    Index   = OldIndex,
                    Item    = OldItem
                }
                : throw new InvalidOperationException($"Cannot convert {nameof(SortedChange)} of type {Type} to {nameof(SortedRemoval<T>)}");

        public SortedReplacement<T> AsReplacement()
            => (Type is SortedChangeType.Replacement)
                ? new()
                {
                    Index   = NewIndex,
                    NewItem = NewItem,
                    OldItem = OldItem
                }
                : throw new InvalidOperationException($"Cannot convert {nameof(SortedChange)} of type {Type} to {nameof(SortedReplacement<T>)}");

        public SortedUpdate<T> AsUpdate()
            => (Type is SortedChangeType.Update)
                ? new()
                {
                    NewIndex    = NewIndex,
                    NewItem     = NewItem,
                    OldIndex    = OldIndex,
                    OldItem     = OldItem
                }
                : throw new InvalidOperationException($"Cannot convert {nameof(SortedChange)} of type {Type} to {nameof(SortedUpdate<T>)}");

        public SortedChange<U>? WithSelectedItems<U>(Func<T, U> itemSelector)
        {
            switch(Type)
            {
                case SortedChangeType.Insertion:
                    return new()
                    {
                        NewIndex    = NewIndex,
                        NewItem     = itemSelector.Invoke(NewItem),
                        Type        = Type
                    };

                case SortedChangeType.Movement:
                    return new()
                    {
                        NewIndex    = NewIndex,
                        NewItem     = itemSelector.Invoke(NewItem),
                        OldIndex    = OldIndex,
                        Type        = Type
                    };

                case SortedChangeType.Removal:
                    return new()
                    {
                        OldIndex    = OldIndex,
                        OldItem     = itemSelector.Invoke(OldItem),
                        Type        = Type
                    };

                case SortedChangeType.Replacement:
                    {
                        var oldItem = itemSelector.Invoke(OldItem);
                        var newItem = itemSelector.Invoke(NewItem);

                        return EqualityComparer<U>.Default.Equals(oldItem, newItem)
                            ? new()
                            {
                                NewIndex    = NewIndex,
                                NewItem     = itemSelector.Invoke(NewItem),
                                OldItem     = itemSelector.Invoke(OldItem),
                                Type        = SortedChangeType.Replacement
                            }
                            : null;
                    }

                case SortedChangeType.Update:
                    {
                        var oldItem = itemSelector.Invoke(OldItem);
                        var newItem = itemSelector.Invoke(NewItem);

                        return new()
                        {
                            NewIndex    = NewIndex,
                            NewItem     = itemSelector.Invoke(NewItem),
                            OldIndex    = OldIndex,
                            OldItem     = itemSelector.Invoke(OldItem),
                            Type        = EqualityComparer<U>.Default.Equals(oldItem, newItem)
                                ? SortedChangeType.Movement
                                : Type
                        };
                    }

                default:
                    throw new InvalidOperationException($"Unsupported {nameof(KeyedChange)} type {Type}");
            }
        }

        private int NewIndex { get; init; }
        
        private T NewItem { get; init; }
        
        private int OldIndex { get; init; }
        
        private T OldItem { get; init; }
    }
}
