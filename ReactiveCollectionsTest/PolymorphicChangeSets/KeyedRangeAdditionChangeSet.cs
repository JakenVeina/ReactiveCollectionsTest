using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ReactiveCollectionsTest.PolymorphicChangeSets
{
    public class KeyedRangeAdditionChangeSet<TKey, TItem>
        : IKeyedChangeSet<TKey, TItem>
    {
        public required ImmutableArray<KeyedAddition<TKey, TItem>> Additions { get; init; }

        public int Count
            => Additions.Length;

        public ChangeSetType Type
            => ChangeSetType.Update;

        public IEnumerator<KeyedChange<TKey, TItem>> GetEnumerator()
        {
            foreach(var addition in Additions)
                yield return KeyedChange.Addition(
                    key:    addition.Key,
                    item:   addition.Item);
        }

        public IKeyedChangeSet<TKey, TItem>? Filter(Predicate<TItem> itemPredicate)
        {
            var additions = ImmutableArray.CreateBuilder<KeyedAddition<TKey, TItem>>(initialCapacity: Additions.Length);
            foreach(var addition in Additions)
                if (itemPredicate.Invoke(addition.Item))
                    additions.Add(addition);

            return (additions.Count is 0)
                ? null
                : new KeyedRangeAdditionChangeSet<TKey, TItem>()
                {
                    Additions = additions.MoveToOrCreateImmutable()
                };
        }

        public IKeyedChangeSet<TKey, TItemOut> Transform<TItemOut>(Func<TItem, TItemOut> itemSelector)
        {
            var additions = ImmutableArray.CreateBuilder<KeyedAddition<TKey, TItemOut>>(initialCapacity: Additions.Length);
            foreach(var addition in Additions)
                additions.Add(new()
                {
                    Item    = itemSelector.Invoke(addition.Item),
                    Key     = addition.Key
                });

            return new KeyedRangeAdditionChangeSet<TKey, TItemOut>()
            {
                Additions = additions.MoveToImmutable()
            };
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
