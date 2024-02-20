using System;
using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

namespace ReactiveCollectionsTest.Benchmarks
{
    public class ListMove
    {
        public const int ItemsCount
            = 10000;

        public const int RngSeed
            = 1234567;

        public ListMove()
            => _list = new();

        [Params(0, 1, 5, 10, 50, 100, 500, 1000, 5000, 9999)]
        public int OldIndex { get; set; }

        [Params(0, 1, 5, 10, 50, 100, 500, 1000, 5000, 9999)]
        public int NewIndex { get; set; }

        [IterationSetup]
        public void IterationSetup()
        {
            _list = new();
            var rng = new Random(RngSeed);
            while(_list.Count < ItemsCount)
                _list.Add(rng.Next(maxValue: ItemsCount / 2));
        }

        [Benchmark(Baseline = true)]
        public void MoveByRemoveInsert()
        {
            if (OldIndex == NewIndex)
                return;

            var item = _list[OldIndex];
            _list.RemoveAt(OldIndex);
            _list.Insert(NewIndex, item);
        }

        [Benchmark]
        public void MoveByShuffle()
        {
            if (OldIndex < NewIndex)
            {
                var targetItem = _list[OldIndex];
                for(var i = OldIndex; i < NewIndex; ++i)
                    _list[i] = _list[i + 1];
                _list[NewIndex] = targetItem;
            }
            else if (OldIndex > NewIndex)
            {
                var targetItem = _list[OldIndex];
                for(var i = OldIndex; i > NewIndex; --i)
                    _list[i] = _list[i - 1];
                _list[NewIndex] = targetItem;
            }
        }

        private List<int> _list;
    }
}
