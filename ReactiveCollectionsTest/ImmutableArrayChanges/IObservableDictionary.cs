using System;
using System.Collections.Generic;
using System.Reactive;

namespace ReactiveCollectionsTest.ImmutableArrayChanges
{
    public interface IObservableDictionary<TKey, TValue>
            : IDictionary<TKey, TValue>,
                IObservable<KeyedChangeSet<TKey, TValue>>
    {
        IObservable<Unit> CollectionChanged { get; }

        new IReadOnlyCollection<TKey> Keys { get; }

        new IReadOnlyCollection<TValue> Values { get; }

        void AddRange(
            IEnumerable<TValue> values,
            Func<TValue, TKey>  keySelector);

        void ApplyChangeSet(KeyedChangeSet<TKey, TValue> changeSet);

        IObservable<TValue> ObserveValue(TKey key);

        void Reset(
            IEnumerable<TValue> values,
            Func<TValue, TKey>  keySelector);
    }
}
