using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Channels;

namespace ReactiveCollectionsTest.ImmutableArrayChanges
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
                                var insertion = change.AsInsertion();
                                list.Add(insertion.Item);
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
                : new()
                {
                    Changes = changes.MoveToOrCreateImmutable(),
                    Type    = changeSet.Type
                };
        }
    }
}
