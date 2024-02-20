using System;
using System.Collections.Generic;

namespace ReactiveCollectionsTest.IReadOnlyListChanges
{
    public static class KeyedChangeSetExtensions
    {
        public static KeyedChangeSet<TKey, TOut>? WithSelectedItems<TKey, TIn, TOut>(
            this    KeyedChangeSet<TKey, TIn>   changeSet,
                    Func<TIn, TOut>             itemSelector)
        {
            var changes = new List<KeyedChange<TKey, TOut>>(capacity: changeSet.Changes.Count);

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
                    Changes = changes,
                    Type    = changeSet.Type
                };
        }
    }
}
