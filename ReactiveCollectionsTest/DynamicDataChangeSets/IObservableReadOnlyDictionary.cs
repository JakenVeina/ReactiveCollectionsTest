using System;
using System.Collections.Generic;
using System.Reactive;

using DynamicData;

namespace ReactiveCollectionsTest.DynamicDataChangeSets
{
    public interface IObservableReadOnlyDictionary<TKey, TValue>
            : IReadOnlyDictionary<TKey, TValue>,
                IObservable<IChangeSet<TValue, TKey>>
        where TKey : notnull
        where TValue : notnull
    {
        IObservable<Unit> CollectionChanged { get; }

        new IReadOnlyCollection<TKey> Keys { get; }

        new IReadOnlyCollection<TValue> Values { get; }

        IObservable<TValue> ObserveValue(TKey key);
    }
}
