using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using BenchmarkDotNet.Attributes;

namespace ReactiveCollectionsTest.Benchmarks
{
    [MemoryDiagnoser]
    public class ListEnumeration
    {
        public const int MaxItemValue
            = 1000;

        public const int RngSeed
            = 1234567;

        [Params(1, 10, 100, 1000)]
        public int ItemsCount { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            var rng = new Random(RngSeed);

            var items = new List<int>(capacity: ItemsCount);
            while(items.Count < ItemsCount)
                items.Add(rng.Next(maxValue: MaxItemValue + 1));

            _array          = items.ToArray();
            _list   = items;
            _immutableArray = items.ToImmutableArray();
            _iList           = items;
            _iEnumerable         = items;
        }

        [Benchmark(Baseline = true)]
        public int IEnumerable_ForEach_Extension()
        {
            var sum = 0;

            _iEnumerable.ForEach(value => sum += value);

            return sum;
        }

        [Benchmark]
        public int Polymorphic_ForEach_ExtensionWithStruct()
        {
            var sum = 0;

            _iEnumerable.PolymorphicForEach(value => sum += value);

            return sum;
        }

        [Benchmark]
        public int IList_ForEach_ExtensionWithStruct()
        {
            var sum = 0;

            _iList.ForEach(value => sum += value);

            return sum;
        }

        [Benchmark]
        public int IEnumerable_ForEach_Plain()
        {
            var sum = 0;

            foreach(var item in _iEnumerable)
                sum += item;

            return sum;
        }

        [Benchmark]
        public int IList_ForEach_Plain()
        {
            var sum = 0;

            foreach(var item in _iList)
                sum += item;

            return sum;
        }

        [Benchmark]
        public int IList_For_Plain()
        {
            var sum = 0;

            for(var i = 0; i < _iList.Count; ++i)
                sum += _iList[i];

            return sum;
        }

        [Benchmark]
        public int List_ForEach_Plain()
        {
            var sum = 0;

            foreach(var item in _list)
                sum += item;

            return sum;
        }

        [Benchmark]
        public int List_For_Plain()
        {
            var sum = 0;

            for(var i = 0; i < _list.Count; ++i)
                sum += _list[i];

            return sum;
        }

        [Benchmark]
        public int Array_ForEach_Plain()
        {
            var sum = 0;

            foreach(var item in _array)
                sum += item;

            return sum;
        }

        [Benchmark]
        public int Array_For_Plain()
        {
            var sum = 0;

            for(var i = 0; i < _array.Length; ++i)
                sum += _array[i];

            return sum;
        }

        [Benchmark]
        public int ImmutableArray_ForEach_Plain()
        {
            var sum = 0;

            foreach(var item in _array)
                sum += item;

            return sum;
        }

        [Benchmark]
        public int ImmutableArray_For_Plain()
        {
            var sum = 0;

            for(var i = 0; i < _immutableArray.Length; ++i)
                sum += _immutableArray[i];

            return sum;
        }

        private int[]               _array              = null!;
        private List<int>           _list               = null!;
        private ImmutableArray<int> _immutableArray;
        private IList<int>          _iList              = null!;
        private IEnumerable<int>    _iEnumerable        = null!;
    }
}
