using System;
using System.Collections;
using System.Collections.Generic;

namespace ReactiveCollectionsTest.PolymorphicChangeSets
{
    public class KeyedAdditionChangeSet<TKey, TItem>
        : IKeyedChangeSet<TKey, TItem>
    {
        public int Count
            => 1;

        public required TItem Item { get; init; }

        public required TKey Key { get; init; }

        public ChangeSetType Type
            => ChangeSetType.Update;

        public IEnumerator<KeyedChange<TKey, TItem>> GetEnumerator()
        {
            yield return KeyedChange.Addition(
                key:    Key,
                item:   Item);
        }
        
        public IKeyedChangeSet<TKey, TItem>? Filter(Predicate<TItem> itemPredicate)
            => itemPredicate.Invoke(Item)
                ? this
                : null;

        public IKeyedChangeSet<TKey, TItemOut> Transform<TItemOut>(Func<TItem, TItemOut> itemSelector)
            => new KeyedAdditionChangeSet<TKey, TItemOut>()
            {
                Item    = itemSelector.Invoke(Item),
                Key     = Key
            };

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
