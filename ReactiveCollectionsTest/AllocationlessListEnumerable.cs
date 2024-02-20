using System.Collections;
using System.Collections.Generic;

namespace ReactiveCollectionsTest
{
    public readonly struct AllocationlessListEnumerable<T>
        : IEnumerable<T>
    {
        public AllocationlessListEnumerable(IList<T> list)
            => _list = list;

        public Enumerator GetEnumerator()
            => new(_list);

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        private readonly IList<T> _list;

        public struct Enumerator
            : IEnumerator<T>
        {
            public Enumerator(IList<T> list)
            {
                _index = -1;
                _list = list;
            }

            public T Current
                => _list[_index];

            public bool MoveNext()
                => (++_index) < _list.Count;

            public void Reset()
                => _index = -1;

            public void Dispose() { }

            object? IEnumerator.Current
                => Current;

            private readonly IList<T> _list;

            private int _index;
        }
    }
}
