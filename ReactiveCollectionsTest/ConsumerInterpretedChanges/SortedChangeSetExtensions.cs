using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ReactiveCollectionsTest.ConsumerInterpretedChanges
{
    public static class SortedChangeSetExtensions
    {
        public static void ApplyTo<T>(
            this    SortedChangeSet<T>  changeSet,
                    IList<T>            list)
        {
            switch(changeSet.Type)
            {
                case ChangeSetType.Clear:
                    list.Clear();
                    break;

                case ChangeSetType.Reset:
                    list.Clear();
                    foreach(var change in changeSet.Changes)
                    {
                        switch(change.Type)
                        {
                            case SortedChangeType.Insertion:
                                list.Add(change.NewItem.Value);
                                break;

                            case SortedChangeType.Removal:
                                break;

                            default:
                                throw new InvalidOperationException($"Unsupported {nameof(SortedChange)} type {change.Type} within {nameof(SortedChangeSet)} of type {changeSet.Type}");
                        }
                    }
                    break;

                case ChangeSetType.Update:
                    foreach(var change in changeSet.Changes)
                    {
                        switch(change.Type)
                        {
                            case SortedChangeType.Insertion:
                                list.Insert(
                                    index:  change.NewIndex.Value,
                                    item:   change.NewItem.Value);
                                break;

                            case SortedChangeType.Movement:
                                list.ShuffleMove(
                                    oldIndex:   change.OldIndex.Value,
                                    newIndex:   change.NewIndex.Value);
                                break;

                            case SortedChangeType.Removal:
                                list.RemoveAt(change.OldIndex.Value);
                                break;

                            case SortedChangeType.Replacement:
                                list[change.NewIndex.Value] = change.NewItem.Value;
                                break;

                            case SortedChangeType.Update:
                                list.ShuffleMove(
                                    oldIndex:   change.OldIndex.Value,
                                    newIndex:   change.NewIndex.Value);
                                list[change.NewIndex.Value] = change.NewItem.Value;
                                break;

                            default:
                                throw new InvalidOperationException($"Unsupported {nameof(SortedChange)} type {change.Type}");
                        }
                    }
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported {nameof(SortedChangeSet)} type {changeSet.Type}");
            }
        }

        public static SortedChangeSet<U>? WithSelectedItems<T, U>(
            this    SortedChangeSet<T>  changeSet,
                    Func<T, U>          itemSelector)
        {
            var changes = ImmutableArray.CreateBuilder<SortedChange<U>>(initialCapacity: changeSet.Changes.Length);

            foreach(var change in changeSet.Changes)
            {
                var newChange = change.WithSelectedItems(itemSelector);
                if (newChange is not null)
                    changes.Add(newChange.Value);
            }

            return (changes.Count is 0)
                ? null
                :  new()
                {
                    Changes = changes.MoveToOrCreateImmutable(),
                    Type    = changeSet.Type
                };
        }
    }
}
