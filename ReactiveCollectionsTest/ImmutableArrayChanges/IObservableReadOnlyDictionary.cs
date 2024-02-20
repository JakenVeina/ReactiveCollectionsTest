using System;
using System.Collections.Generic;
using System.Reactive;

namespace ReactiveCollectionsTest.ImmutableArrayChanges
{
    public interface IObservableReadOnlyDictionary<TKey, TValue>
            : IReadOnlyDictionary<TKey, TValue>,
                IObservable<KeyedChangeSet<TKey, TValue>>
    {
        IObservable<Unit> CollectionChanged { get; }

        new IReadOnlyCollection<TKey> Keys { get; }

        new IReadOnlyCollection<TValue> Values { get; }

        IObservable<TValue> ObserveValue(TKey key);
    }
}
