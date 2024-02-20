using System.Collections.Generic;

namespace ReactiveCollectionsTest
{
    public static class ListExtensions
    {
        public static int FindSortingIndex<T>(
            this    IList<T>        sortedItems,
                    T               item,
                    IComparer<T>    itemComparer)
        {
            var searchRangeStartIndex = 0;
            var searchRangeEndIndex = sortedItems.Count - 1;
            int result = 0;

            while (searchRangeEndIndex >= searchRangeStartIndex)
            {
                result = (searchRangeStartIndex + searchRangeEndIndex) / 2;

                var comparison = itemComparer.Compare(item, sortedItems[result]);
                if (comparison < 0)
                    searchRangeEndIndex = result - 1;
                else if (comparison > 0)
                {
                    ++result;
                    searchRangeStartIndex = result;
                }
                else
                    return result;
            }

            return result;
        }

        public static T ShuffleMove<T>(
            this    IList<T>    list,
                    int         oldIndex,
                    int         newIndex)
        {
            var item = list[oldIndex];

            if (oldIndex < newIndex)
            {
                var targetItem = list[oldIndex];
                for(var i = oldIndex; i < newIndex; ++i)
                    list[i] = list[i + 1];
                list[newIndex] = targetItem;
            }
            else if (oldIndex > newIndex)
            {
                var targetItem = list[oldIndex];
                for(var i = oldIndex; i > newIndex; --i)
                    list[i] = list[i - 1];
                list[newIndex] = targetItem;
            }

            return item;
        }
    }
}
