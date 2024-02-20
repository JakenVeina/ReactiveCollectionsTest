using System;
using System.Collections;
using System.Collections.Generic;

namespace ReactiveCollectionsTest.PolymorphicChangeSets
{
    public class SortedMovementChangeSet<T>
        : ISortedChangeSet<T>
    {
        public int Count
            => 1;

        public required T Item { get; init; }

        public required int NewIndex { get; init; }
        
        public required int OldIndex { get; init; }

        public ChangeSetType Type
            => ChangeSetType.Update;

        public IEnumerator<SortedChange<T>> GetEnumerator()
        {
            yield return SortedChange.Movement(
                oldIndex:   OldIndex,
                newIndex:   NewIndex,
                item:       Item);
        }

        public ISortedChangeSet<U> Transform<U>(Func<T, U> itemSelector)
            => new SortedMovementChangeSet<U>()
                {
                    Item        = itemSelector.Invoke(Item),
                    NewIndex    = NewIndex,
                    OldIndex    = OldIndex
                };

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
