using System;
using System.Collections;
using System.Collections.Generic;

namespace ReactiveCollectionsTest.PolymorphicChangeSets
{
    public class KeyedReplacementChangeSet<TKey, TItem>
        : IKeyedChangeSet<TKey, TItem>
    {
        public int Count
            => 1;

        public required TKey Key { get; init; }

        public required TItem OldItem { get; init; }

        public required TItem NewItem { get; init; }

        public ChangeSetType Type
            => ChangeSetType.Update;

        public IEnumerator<KeyedChange<TKey, TItem>> GetEnumerator()
        {
            yield return KeyedChange.Replacement(
                key:        Key,
                oldItem:    OldItem,
                newItem:    NewItem);
        }

        public IKeyedChangeSet<TKey, TItem>? Filter(Predicate<TItem> itemPredicate)
        {
            var wasIncluded = itemPredicate.Invoke(OldItem);
            var isIncluded = itemPredicate.Invoke(NewItem);

            return (wasIncluded, isIncluded) switch
            {
                (true, true)    => this,
                (true, false)   => new KeyedRemovalChangeSet<TKey, TItem>()
                {
                    Item    = OldItem,
                    Key     = Key
                },
                (false, true)   => new KeyedAdditionChangeSet<TKey, TItem>()
                {
                    Item    = NewItem,
                    Key     = Key
                },
                _               => null
            };
        }

        public IKeyedChangeSet<TKey, TItemOut>? Transform<TItemOut>(Func<TItem, TItemOut> itemSelector)
        {
            var newItem = itemSelector.Invoke(NewItem);
            var oldItem = itemSelector.Invoke(OldItem);

            return EqualityComparer<TItemOut>.Default.Equals(newItem, oldItem)
                ? null
                : new KeyedReplacementChangeSet<TKey, TItemOut>()
                {
                    Key     = Key,
                    NewItem = newItem,
                    OldItem = oldItem
                };
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
