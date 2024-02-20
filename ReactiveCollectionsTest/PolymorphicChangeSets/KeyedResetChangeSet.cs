using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

using DynamicData;

namespace ReactiveCollectionsTest.PolymorphicChangeSets
{
    public class KeyedResetChangeSet<TKey, TItem>
        : IKeyedChangeSet<TKey, TItem>
    {
        public int Count
            => Additions.Length + Removals.Length;

        public required ImmutableArray<KeyedAddition<TKey, TItem>> Additions { get; init; }
        
        public required ImmutableArray<KeyedRemoval<TKey, TItem>> Removals { get; init; }

        public ChangeSetType Type
            => ChangeSetType.Reset;

        public IEnumerator<KeyedChange<TKey, TItem>> GetEnumerator()
        {
            foreach(var removal in Removals)
                yield return KeyedChange.Removal(
                    key:    removal.Key,
                    item:   removal.Item);

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

            var removals = ImmutableArray.CreateBuilder<KeyedRemoval<TKey, TItem>>(initialCapacity: Removals.Length);
            foreach(var removal in Removals)
                if (itemPredicate.Invoke(removal.Item))
                    removals.Add(removal);

            return (additions.Count, removals.Count) switch
            {
                (0, 0)  => null,
                (1, 0)  => new KeyedAdditionChangeSet<TKey, TItem>()
                {
                    Item    = additions[0].Item,
                    Key     = additions[0].Key
                },
                (>1, 0) => new KeyedRangeAdditionChangeSet<TKey, TItem>()
                {
                    Additions = additions.MoveToOrCreateImmutable()
                },
                (0, 1)  => new KeyedRemovalChangeSet<TKey, TItem>()
                {
                    Item    = removals[0].Item,
                    Key     = removals[0].Key
                },
                _       => new KeyedResetChangeSet<TKey, TItem>()
                {
                    Additions   = additions.MoveToOrCreateImmutable(),
                    Removals    = removals.MoveToOrCreateImmutable()
                }
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

            var removals = ImmutableArray.CreateBuilder<KeyedRemoval<TKey, TItemOut>>(initialCapacity: Removals.Length);
            foreach(var removal in Removals)
                removals.Add(new()
                {
                    Item    = itemSelector.Invoke(removal.Item),
                    Key     = removal.Key
                });

            return new KeyedResetChangeSet<TKey, TItemOut>()
            {
                Additions   = additions.MoveToImmutable(),
                Removals    = removals.MoveToImmutable()
            };
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
