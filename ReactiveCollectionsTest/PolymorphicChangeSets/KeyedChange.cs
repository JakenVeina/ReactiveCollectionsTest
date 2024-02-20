using System;

namespace ReactiveCollectionsTest.PolymorphicChangeSets
{
    public static class KeyedChange
    {
        public static KeyedChange<TKey, TItem> Addition<TKey, TItem>(
                TKey    key,
                TItem   item)
            => KeyedChange<TKey, TItem>.Addition(
                key:    key,
                item:   item);

        public static KeyedChange<TKey, TItem> Removal<TKey, TItem>(
                TKey    key,
                TItem   item)
            => KeyedChange<TKey, TItem>.Removal(
                key:    key,
                item:   item);

        public static KeyedChange<TKey, TItem> Replacement<TKey, TItem>(
                TKey    key,
                TItem   oldItem,
                TItem   newItem)
            => KeyedChange<TKey, TItem>.Replacement(
                key:        key,
                oldItem:    oldItem,
                newItem:    newItem);
    }

    public readonly record struct KeyedChange<TKey, TItem>
    {
        public static KeyedChange<TKey, TItem> Addition(
                TKey    key,
                TItem   item)
            => new()
            {
                Key     = key,
                NewItem = item,
                Type    = KeyedChangeType.Addition
            };

        public static KeyedChange<TKey, TItem> Removal(
                TKey    key,
                TItem   item)
            => new()
            {
                Key     = key,
                OldItem = item,
                Type    = KeyedChangeType.Removal
            };

        public static KeyedChange<TKey, TItem> Replacement(
                TKey    key,
                TItem   oldItem,
                TItem   newItem)
            => new()
            {
                Key     = key,
                OldItem = oldItem,
                NewItem = newItem,
                Type    = KeyedChangeType.Replacement
            };

        public KeyedChangeType Type { get; private init; }

        public KeyedAddition<TKey, TItem> AsAddition()
            => (Type is KeyedChangeType.Addition)
                ? new()
                {
                    Item    = NewItem,
                    Key     = Key
                }
                : throw new InvalidOperationException($"Cannot convert {nameof(KeyedChange)} of type {Type} to {nameof(KeyedAddition<TKey, TItem>)}");

        public KeyedRemoval<TKey, TItem> AsRemoval()
            => (Type is KeyedChangeType.Removal)
                ? new()
                {
                    Item    = OldItem,
                    Key     = Key
                }
                : throw new InvalidOperationException($"Cannot convert {nameof(KeyedChange)} of type {Type} to {nameof(KeyedRemoval<TKey, TItem>)}");

        public KeyedReplacement<TKey, TItem> AsReplacement()
            => (Type is KeyedChangeType.Replacement)
                ? new()
                {
                    Key     = Key,
                    NewItem = NewItem,
                    OldItem = OldItem
                }
                : throw new InvalidOperationException($"Cannot convert {nameof(KeyedChange)} of type {Type} to {nameof(KeyedReplacement<TKey, TItem>)}");

        private TKey Key { get; init; }

        private TItem NewItem { get; init; }
        
        private TItem OldItem { get; init; }
    }
}
