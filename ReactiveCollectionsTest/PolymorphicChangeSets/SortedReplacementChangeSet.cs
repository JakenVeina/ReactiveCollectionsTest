using System;
using System.Collections;
using System.Collections.Generic;

namespace ReactiveCollectionsTest.PolymorphicChangeSets
{
    public class SortedReplacementChangeSet<T>
        : ISortedChangeSet<T>
    {
        public int Count
            => 1;

        public required int Index { get; init; }
        
        public required T NewItem { get; init; }
        
        public required T OldItem { get; init; }

        public ChangeSetType Type
            => ChangeSetType.Update;

        public IEnumerator<SortedChange<T>> GetEnumerator()
        {
            yield return SortedChange.Replacement(
                index:      Index,
                oldItem:    OldItem,
                newItem:    NewItem);
        }

        public ISortedChangeSet<U>? Transform<U>(Func<T, U> itemSelector)
            => new SortedReplacementChangeSet<U>()
                {
                    Index   = Index,
                    NewItem = itemSelector.Invoke(NewItem),
                    OldItem = itemSelector.Invoke(OldItem)
                };

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
