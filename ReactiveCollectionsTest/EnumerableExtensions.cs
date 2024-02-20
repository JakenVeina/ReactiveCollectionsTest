using System;
using System.Collections.Generic;

namespace ReactiveCollectionsTest
{
    public static class EnumerableExtensions
    {
        public static void ForEach<T>(
            this    IEnumerable<T>  source,
                    Action<T>       action)
        {
            foreach(var item in source)
                action.Invoke(item);
        }

        public static void ForEach<T>(
            this    IList<T>    source,
                    Action<T>   action)
        {
            foreach(var item in new AllocationlessListEnumerable<T>(source))
                action.Invoke(item);
        }

        public static void PolymorphicForEach<T>(
            this    IEnumerable<T>  source,
                    Action<T>       action)
        {
            if (source is IList<T> list)
                foreach(var item in new AllocationlessListEnumerable<T>(list))
                    action.Invoke(item);
            else
                foreach(var item in source)
                    action.Invoke(item);
        }
    }
}
