using System;
using System.Collections.Generic;
using System.Reactive;

namespace ReactiveCollectionsTest.PolymorphicChangeSets
{
    public interface IObservableDictionary<TKey, TValue>
            : IDictionary<TKey, TValue>,
                IObservable<IKeyedChangeSet<TKey, TValue>>
    {
        IObservable<Unit> CollectionChanged { get; }

        new IReadOnlyCollection<TKey> Keys { get; }

        new IReadOnlyCollection<TValue> Values { get; }

        void AddRange(
            IEnumerable<TValue> values,
            Func<TValue, TKey>  keySelector);

        void ApplyChangeSet(IKeyedChangeSet<TKey, TValue> changeSet);

        IObservable<TValue> ObserveValue(TKey key);

        void Reset(
            IEnumerable<TValue> values,
            Func<TValue, TKey>  keySelector);
    }
}
