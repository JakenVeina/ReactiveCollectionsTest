using System;
using System.Collections.Immutable;

namespace ReactiveCollectionsTest.ConsumerInterpretedChanges
{
    public static class KeyedChangeSetExtensions
    {
        public static KeyedChangeSet<TKey, TNewItem>? WithSelectedItems<TKey, TItem, TNewItem>(
            this    KeyedChangeSet<TKey, TItem> changeSet,
                    Func<TItem, TNewItem>       itemSelector)
        {
            var changes = ImmutableArray.CreateBuilder<KeyedChange<TKey, TNewItem>>(initialCapacity: changeSet.Changes.Length);

            foreach(var change in changeSet.Changes)
            {
                var newChange = change.WithSelectedItems(itemSelector);
                if (newChange is not null)
                    changes.Add(newChange.Value);
            }

            return (changes.Count is 0)
                ? null
                : new()
                {
                    Changes = changes.MoveToOrCreateImmutable(),
                    Type    = changeSet.Type
                };
        }
    }
}
