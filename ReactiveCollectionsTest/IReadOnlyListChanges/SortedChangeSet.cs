using System;
using System.Collections.Generic;

namespace ReactiveCollectionsTest.IReadOnlyListChanges
{
    public static class SortedChangeSet
    {
        public static SortedChangeSet<T> Clear<T>(IReadOnlyList<T> items)
        {
            var changes = new List<SortedChange<T>>(capacity: items.Count);

            // List changes in reverse order, to preserve correctness of indexing.
            for(var index = items.Count - 1; index >= 0; --index)
                changes.Add(SortedChange.Removal(
                    index:  index,
                    item:   items[index]));

            return new()
            {
                Changes = changes,
                Type    = ChangeSetType.Clear
            };
        }

        public static SortedChangeSet<T> Reset<T>(
            IReadOnlyList<T> oldSortedItems,
            IReadOnlyList<T> newSortedItems)
        {
            var changes = new List<SortedChange<T>>(capacity: oldSortedItems.Count + newSortedItems.Count);

            for(var index = oldSortedItems.Count - 1; index >= 0; --index)
                changes.Add(SortedChange.Removal(
                    index:  index,
                    item:   oldSortedItems[index]));

            for(var index = 0; index < newSortedItems.Count; ++index)
                changes.Add(SortedChange.Insertion(
                    index:  index,
                    item:   newSortedItems[index]));

            return new()
            {
                Changes = changes,
                Type    = ChangeSetType.Reset
            };
        }
    }

    public readonly record struct SortedChangeSet<T>
    {
        public required IReadOnlyList<SortedChange<T>> Changes { get; init; }

        public required ChangeSetType Type { get; init; }
    }
}
