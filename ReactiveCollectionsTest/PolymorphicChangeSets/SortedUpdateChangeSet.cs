using System;
using System.Collections;
using System.Collections.Generic;

namespace ReactiveCollectionsTest.PolymorphicChangeSets
{
    public class SortedUpdateChangeSet<T>
        : ISortedChangeSet<T>
    {
        public int Count
            => 1;

        public required int NewIndex { get; init; }
        
        public required T NewItem { get; init; }
        
        public required int OldIndex { get; init; }

        public required T OldItem { get; init; }

        public ChangeSetType Type
            => ChangeSetType.Update;

        public IEnumerator<SortedChange<T>> GetEnumerator()
        {
            yield return SortedChange.Update(
                oldIndex:   OldIndex,
                newIndex:   NewIndex,
                oldItem:    OldItem,
                newItem:    NewItem);
        }

        public ISortedChangeSet<U>? Transform<U>(Func<T, U> itemSelector)
            => new SortedUpdateChangeSet<U>()
                {
                    OldIndex    = OldIndex,
                    NewIndex    = NewIndex,
                    NewItem     = itemSelector.Invoke(NewItem),
                    OldItem     = itemSelector.Invoke(OldItem)
                };

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
