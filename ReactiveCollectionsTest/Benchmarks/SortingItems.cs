using System;
using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

namespace ReactiveCollectionsTest.Benchmarks
{
    public class SortingItems
    {
        public const int RngSeed
            = 1234567;

        public SortingItems()
        {
            _source         = new();
            _destination    = new();
        }

        [Params(1, 10, 100, 1000, 10000, 100000)]
        public int ItemsCount { get; set; }

        [IterationSetup]
        public void IterationSetup()
        {
            _source = new(capacity: ItemsCount);
            var rng = new Random(RngSeed);
            while(_source.Count < ItemsCount)
                _source.Add(rng.Next(maxValue: ItemsCount / 2));

            _destination    = new(capacity: ItemsCount);
        }

        [Benchmark(Baseline = true)]
        public void ListSort()
        {
            foreach(var item in _source)
                _destination.Add(item);
            _destination.Sort(Comparer<int>.Default);
        }

        [Benchmark]
        public void FindSortingIndex()
        {
            foreach(var item in _source)
                _destination.Insert(
                    index:  _destination.FindSortingIndex(item, Comparer<int>.Default),
                    item:   item);
        }

        private List<int>   _source;
        private List<int>   _destination;
    }
}
