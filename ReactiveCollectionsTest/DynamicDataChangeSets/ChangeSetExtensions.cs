using System;
using System.Collections.Generic;

using DynamicData;

namespace ReactiveCollectionsTest.DynamicDataChangeSets
{
    public static class ChangeSetExtensions
    {
        public static void ApplyTo<T>(
                this    IChangeSet<T>   changeSet,
                        IList<T>        list)
            where T : notnull
        {
            foreach(var change in changeSet)
            {
                switch(change.Reason)
                {
                    case ListChangeReason.Add:
                        list.Insert(
                            index:  change.Item.CurrentIndex,
                            item:   change.Item.Current);
                        break;

                    case ListChangeReason.Moved:
                        list.ShuffleMove(
                            oldIndex:   change.Item.PreviousIndex,
                            newIndex:   change.Item.CurrentIndex);
                        break;

                    case ListChangeReason.Remove:
                        list.RemoveAt(change.Item.CurrentIndex);
                        break;

                    case ListChangeReason.Replace:
                        if (change.Item.PreviousIndex != change.Item.CurrentIndex)
                            list.ShuffleMove(
                                oldIndex: change.Item.PreviousIndex,
                                newIndex: change.Item.CurrentIndex);

                        list[change.Item.CurrentIndex] = change.Item.Current;
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported {nameof(ListChangeReason)} value {change.Reason}");
                }
            }
        }
    }
}
