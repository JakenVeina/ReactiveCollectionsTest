using System;
using System.Collections.Immutable;

namespace ReactiveCollectionsTest.ImmutableArrayChanges
{
    public static class KeyedChangeSetExtensions
    {
        public static KeyedChangeSet<TKey, TOut>? WithSelectedItems<TKey, TIn, TOut>(
            this    KeyedChangeSet<TKey, TIn>   changeSet,
                    Func<TIn, TOut>             itemSelector)
        {
            var changes = ImmutableArray.CreateBuilder<KeyedChange<TKey, TOut>>(initialCapacity: changeSet.Changes.Length);

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
                    Changes = changes.MoveToImmutable(),
                    Type    = changeSet.Type
                };
        }
    }
}
