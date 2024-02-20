using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace ReactiveCollectionsTest.PolymorphicChangeSets
{
    public class KeyedClearChangeSet<TKey, TItem>
        : IKeyedChangeSet<TKey, TItem>
    {
        public int Count
            => Removals.Length;

        public required ImmutableArray<KeyedRemoval<TKey, TItem>> Removals { get; init; }

        public ChangeSetType Type
            => ChangeSetType.Clear;

        public IEnumerator<KeyedChange<TKey, TItem>> GetEnumerator()
        {
            foreach(var removal in Removals)
                yield return KeyedChange.Removal(
                    key:    removal.Key,
                    item:   removal.Item);
        }

        public IKeyedChangeSet<TKey, TItem>? Filter(Predicate<TItem> itemPredicate)
        {
            var removals = ImmutableArray.CreateBuilder<KeyedRemoval<TKey, TItem>>(initialCapacity: Removals.Length);
            foreach(var removal in Removals)
                if (itemPredicate.Invoke(removal.Item))
                    removals.Add(removal);

            return (removals.Count is 0)
                ? null
                : new KeyedClearChangeSet<TKey, TItem>()
                {
                    Removals = removals.MoveToOrCreateImmutable()
                };
        }

        public IKeyedChangeSet<TKey, TItemOut> Transform<TItemOut>(Func<TItem, TItemOut> itemSelector)
        {
            var removals = ImmutableArray.CreateBuilder<KeyedRemoval<TKey, TItemOut>>(initialCapacity: Removals.Length);
            foreach(var removal in Removals)
                removals.Add(new()
                {
                    Item    = itemSelector.Invoke(removal.Item),
                    Key     = removal.Key
                });

            return new KeyedClearChangeSet<TKey, TItemOut>()
            {
                Removals = removals.MoveToImmutable()
            };
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
