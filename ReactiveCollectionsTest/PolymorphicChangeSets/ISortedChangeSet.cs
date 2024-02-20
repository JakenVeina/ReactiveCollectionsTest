using System;
using System.Collections.Generic;

namespace ReactiveCollectionsTest.PolymorphicChangeSets
{
    public interface ISortedChangeSet<T>
        : IEnumerable<SortedChange<T>>
    {
        int Count { get; }

        ChangeSetType Type { get; }

        ISortedChangeSet<U>? Transform<U>(Func<T, U> itemSelector);
    }
}
