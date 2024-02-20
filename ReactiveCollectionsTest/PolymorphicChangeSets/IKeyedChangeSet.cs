using System;
using System.Collections.Generic;

namespace ReactiveCollectionsTest.PolymorphicChangeSets
{
    public interface IKeyedChangeSet<TKey, TItem>
        : IEnumerable<KeyedChange<TKey, TItem>>
    {
        int Count { get; }

        ChangeSetType Type { get; }

        IKeyedChangeSet<TKey, TItem>? Filter(Predicate<TItem> itemPredicate);

        IKeyedChangeSet<TKey, TItemOut>? Transform<TItemOut>(Func<TItem, TItemOut> itemSelector);
    }
}
