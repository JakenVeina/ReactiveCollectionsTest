using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ReactiveCollectionsTest.ConsumerInterpretedChanges
{
    public static class KeyedChangeSet
    {
        public static KeyedChangeSet<TKey, TItem> Addition<TKey, TItem>(
                TKey    key,
                TItem   item)
            => new()
            {
                Changes = ImmutableArray.Create(KeyedChange.Addition(
                    key:    key,
                    item:   item)),
                Type    = ChangeSetType.Update
            };

        public static KeyedChangeSet<TKey, TItem> Addition<TKey, TItem>(IEnumerable<KeyValuePair<TKey, TItem>> items)
        {
            if (!items.TryGetNonEnumeratedCount(out var itemsCount))
                itemsCount = 0;

            var changes = ImmutableArray.CreateBuilder<KeyedChange<TKey, TItem>>(initialCapacity: itemsCount);

            foreach(var pair in items)
                changes.Add(KeyedChange.Addition(
                    key:    pair.Key,
                    item:   pair.Value));

            return new()
            {
                Changes = changes.MoveToOrCreateImmutable(),
                Type    = ChangeSetType.Update
            };
        }

        public static KeyedChangeSet<TKey, TItem> Clear<TKey, TItem>(IEnumerable<KeyValuePair<TKey, TItem>> items)
        {
            if (!items.TryGetNonEnumeratedCount(out var itemsCount))
                itemsCount = 0;

            var changes = ImmutableArray.CreateBuilder<KeyedChange<TKey, TItem>>(initialCapacity: itemsCount);

            foreach(var pair in items)
                changes.Add(KeyedChange.Removal(
                    key:    pair.Key,
                    item:   pair.Value));

            return new()
            {
                Changes = changes.MoveToOrCreateImmutable(),
                Type    = ChangeSetType.Clear
            };
        }

        public static KeyedChangeSet<TKey, TItem> Removal<TKey, TItem>(
                TKey    key,
                TItem   item)
            => new()
            {
                Changes = ImmutableArray.Create(KeyedChange.Removal(
                    key:    key,
                    item:   item)),
                Type    = ChangeSetType.Update
            };

        public static KeyedChangeSet<TKey, TItem> Replacement<TKey, TItem>(
                TKey    key,
                TItem   oldItem,
                TItem   newItem)
            => new()
            {
                Changes = ImmutableArray.Create(KeyedChange.Replacement(
                    key:        key,
                    oldItem:    oldItem,
                    newItem:    newItem)),
                Type    = ChangeSetType.Update
            };

        public static KeyedChangeSet<TKey, TItem> Reset<TKey, TItem>(
            IReadOnlyCollection<KeyValuePair<TKey, TItem>>  oldItems,
            IReadOnlyCollection<KeyValuePair<TKey, TItem>>  newItems)
        {
            var changes = ImmutableArray.CreateBuilder<KeyedChange<TKey, TItem>>(initialCapacity: oldItems.Count + newItems.Count);

            foreach(var pair in oldItems)
                changes.Add(KeyedChange.Removal(
                    key:    pair.Key,
                    item:   pair.Value));

            foreach(var pair in newItems)
                changes.Add(KeyedChange.Addition(
                    key:    pair.Key,
                    item:   pair.Value));

            return new()
            {
                Changes = changes.MoveToImmutable(),
                Type    = ChangeSetType.Reset
            };
        }
    }

    public readonly record struct KeyedChangeSet<TKey, TItem>
    {
        public required ImmutableArray<KeyedChange<TKey, TItem>> Changes { get; init; }

        public required ChangeSetType Type { get; init; }
    }
}
