using System;
using System.Collections.Generic;
using System.Reactive;

using DynamicData;

namespace ReactiveCollectionsTest.DynamicDataChangeSets
{
    public interface IObservableDictionary<TKey, TValue>
            : IDictionary<TKey, TValue>,
                IObservable<IChangeSet<TValue, TKey>>
        where TKey : notnull
        where TValue : notnull
    {
        IObservable<Unit> CollectionChanged { get; }

        new IReadOnlyCollection<TKey> Keys { get; }

        new IReadOnlyCollection<TValue> Values { get; }

        void AddRange(
            IEnumerable<TValue> values,
            Func<TValue, TKey>  keySelector);

        void ApplyChangeSet(IChangeSet<TValue, TKey> changeSet);

        IObservable<TValue> ObserveValue(TKey key);

        void Reset(
            IEnumerable<TValue> values,
            Func<TValue, TKey>  keySelector);
    }
}
