using System;
using System.Collections.Generic;

namespace ReactiveCollectionsTest.PolymorphicChangeSets
{
    public static class SortedChangeSetExtensions
    {
        public static void ApplyTo<T>(
            this    ISortedChangeSet<T> changeSet,
                    IList<T>            list)
        {
            switch(changeSet)
            {
                case { Type: ChangeSetType.Clear }:
                    list.Clear();
                    break;

                case SortedResetChangeSet<T> reset:
                    list.Clear();
                    foreach(var item in reset.NewItems)
                        list.Add(item);
                    break;

                case { Type: ChangeSetType.Update }:
                    foreach(var change in changeSet)
                    {
                        switch(change.Type)
                        {
                            case SortedChangeType.Insertion:
                                var insertion = change.AsInsertion();
                                list.Insert(
                                    index:  insertion.Index,
                                    item:   insertion.Item);
                                break;

                            case SortedChangeType.Movement:
                                var movement = change.AsMovement();
                                list.ShuffleMove(
                                    oldIndex:   movement.OldIndex,
                                    newIndex:   movement.NewIndex);
                                break;

                            case SortedChangeType.Removal:
                                var removal = change.AsRemoval();
                                list.RemoveAt(removal.Index);
                                break;

                            case SortedChangeType.Replacement:
                                var replacement = change.AsReplacement();
                                list[replacement.Index] = replacement.NewItem;
                                break;

                            case SortedChangeType.Update:
                                var update = change.AsUpdate();
                                list.ShuffleMove(
                                    oldIndex:   update.OldIndex,
                                    newIndex:   update.NewIndex);
                                list[update.NewIndex] = update.NewItem;
                                break;

                            default:
                                throw new InvalidOperationException($"Unsupported {nameof(SortedChange)} type {change.Type}");
                        }
                    }
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported {nameof(ISortedChangeSet<T>)} type {changeSet.GetType()}");
            }
        }
    }
}
