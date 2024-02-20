using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ReactiveCollectionsTest.PolymorphicChangeSets
{
    public class SortedClearChangeSet<T>
        : ISortedChangeSet<T>
    {
        public int Count
            => Items.Length;

        public required ImmutableArray<T> Items { get; init; }

        public ChangeSetType Type
            => ChangeSetType.Clear;

        public IEnumerator<SortedChange<T>> GetEnumerator()
        {
            for (var i = Items.Length - 1; i >= 0; --i)
                yield return SortedChange.Removal(
                    index:  i,
                    item:   Items[i]);
        }

        public ISortedChangeSet<U> Transform<U>(Func<T, U> itemSelector)
        {
            var items = ImmutableArray.CreateBuilder<U>(initialCapacity: Items.Length);
            foreach(var item in Items)
                items.Add(itemSelector.Invoke(item));

            return new SortedClearChangeSet<U>()
            {
                Items = items.MoveToImmutable()
            };
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
