using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ReactiveCollectionsTest.PolymorphicChangeSets
{
    public class SortedRangeAdditionChangeSet<T>
        : ISortedChangeSet<T>
    {
        public required ImmutableArray<SortedInsertion<T>> Insertions { get; init; }

        public int Count
            => Insertions.Length;

        public ChangeSetType Type
            => ChangeSetType.Update;

        public IEnumerator<SortedChange<T>> GetEnumerator()
        {
            foreach(var insertion in Insertions)
                yield return SortedChange.Insertion(
                    index:  insertion.Index,
                    item:   insertion.Item);
        }

        public ISortedChangeSet<U> Transform<U>(Func<T, U> itemSelector)
        {
            var insertions = ImmutableArray.CreateBuilder<SortedInsertion<U>>(initialCapacity: Insertions.Length);
            foreach(var insertion in Insertions)
                insertions.Add(new()
                {
                    Index   = insertion.Index,
                    Item    = itemSelector.Invoke(insertion.Item)
                });

            return new SortedRangeAdditionChangeSet<U>()
            {
                Insertions = insertions.MoveToImmutable()
            };
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
