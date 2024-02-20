using System;
using System.Collections.Generic;

namespace ReactiveCollectionsTest.ConsumerInterpretedChanges
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

        public TKey Key { get; private init; }

        public Optional<TItem> NewItem { get; private init; }
        
        public Optional<TItem> OldItem { get; private init; }

        public KeyedChangeType Type { get; private init; }

        public KeyedChange<TKey, TNewItem>? WithSelectedItems<TNewItem>(Func<TItem, TNewItem> itemSelector)
        {
            var newItem = NewItem.IsSpecified
                ? itemSelector.Invoke(NewItem.Value)
                : Optional<TNewItem>.Unspecified;

            var oldItem = OldItem.IsSpecified
                ? itemSelector.Invoke(OldItem.Value)
                : Optional<TNewItem>.Unspecified;

            return ((Type is KeyedChangeType.Replacement) && EqualityComparer<TNewItem>.Default.Equals(newItem.Value, oldItem.Value))
                ? null
                : new()
                {
                    Key     = Key,
                    NewItem = newItem,
                    OldItem = oldItem,
                    Type    = Type
                };
        }
    }
}
