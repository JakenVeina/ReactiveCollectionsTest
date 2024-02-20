using System;
using System.Collections;
using System.Collections.Generic;

namespace ReactiveCollectionsTest.PolymorphicChangeSets
{
    public class SortedRemovalChangeSet<T>
        : ISortedChangeSet<T>
    {
        public int Count
            => 1;

        public required int Index { get; init; }
        
        public required T Item { get; init; }

        public ChangeSetType Type
            => ChangeSetType.Update;

        public IEnumerator<SortedChange<T>> GetEnumerator()
        {
            yield return SortedChange.Removal(
                index:  Index,
                item:   Item);
        }

        public ISortedChangeSet<U>? Transform<U>(Func<T, U> itemSelector)
            => new SortedRemovalChangeSet<U>()
                {
                    Index   = Index,
                    Item    = itemSelector.Invoke(Item),
                };

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
