using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ReactiveCollectionsTest.PolymorphicChangeSets
{
    public class SortedResetChangeSet<T>
        : ISortedChangeSet<T>
    {
        public int Count
            => NewItems.Length + OldItems.Length;

        public required ImmutableArray<T> NewItems { get; init; }
        
        public required ImmutableArray<T> OldItems { get; init; }

        public ChangeSetType Type
            => ChangeSetType.Reset;

        public IEnumerator<SortedChange<T>> GetEnumerator()
        {
            for (var i = OldItems.Length - 1; i >= 0; --i)
                yield return SortedChange.Removal(
                    index:  i,
                    item: OldItems[i]);

            for (var i = 0; i < NewItems.Length; ++i)
                yield return SortedChange.Insertion(
                    index:  i,
                    item: NewItems[i]);
        }

        public ISortedChangeSet<U> Transform<U>(Func<T, U> itemSelector)
        {
            var newItems = ImmutableArray.CreateBuilder<U>(initialCapacity: NewItems.Length);
            foreach(var item in NewItems)
                newItems.Add(itemSelector.Invoke(item));

            var oldItems = ImmutableArray.CreateBuilder<U>(initialCapacity: OldItems.Length);
            foreach(var item in OldItems)
                oldItems.Add(itemSelector.Invoke(item));

            return new SortedResetChangeSet<U>()
            {
                NewItems    = newItems.MoveToImmutable(),
                OldItems    = oldItems.MoveToImmutable()
            };
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
