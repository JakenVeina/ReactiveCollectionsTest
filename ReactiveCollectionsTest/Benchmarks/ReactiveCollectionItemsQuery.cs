using System;
using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

using ReactiveCollectionsTest.ConsumerInterpretedChanges;
using ReactiveCollectionsTest.DynamicDataChangeSets;
using ReactiveCollectionsTest.ImmutableArrayChanges;
using ReactiveCollectionsTest.IReadOnlyListChanges;
using ReactiveCollectionsTest.PolymorphicChangeSets;

namespace ReactiveCollectionsTest.Benchmarks
{
    [MemoryDiagnoser]
    public class ReactiveCollectionItemsQuery
    {
        public const int MaxChangeSetSize
            = 100;

        public const int MaxDataValue
            = 1000;

        public const int OperationCount
            = 10000;

        public const int RngSeed
            = 1234567;

        public const int ThresholdDataValue
            = 500;

        public ReactiveCollectionItemsQuery()
        {
            var rng = new Random(RngSeed);
            var operations = new List<OperationBase>(capacity: OperationCount);
            var nextModelId = 1;

            for(var operationIndex = 0; operationIndex < OperationCount; ++operationIndex)
            {
                switch(_operationTypes[rng.Next(_operationTypes.Count)])
                {
                    case OperationType.Add:
                        operations.Add(new AddOperation()
                        {
                            Model = new()
                            {
                                Id      = nextModelId++,
                                Value   = rng.Next(MaxDataValue + 1)
                            }
                        });
                        break;

                    case OperationType.AddRange:
                        {
                            var modelsCount = rng.Next(minValue: 1, maxValue: MaxChangeSetSize + 1);
                            var models = new List<DataModel>(capacity: modelsCount);
                            while(models.Count < modelsCount)
                                models.Add(new()
                                {
                                    Id      = nextModelId++,
                                    Value   = rng.Next(maxValue: MaxDataValue + 1)
                                });

                            operations.Add(new AddRangeOperation()
                            {
                                Models = models
                            });
                        }
                        break;

                    case OperationType.Clear:
                        operations.Add(new ClearOperation());
                        nextModelId = 1;
                        break;

                    case OperationType.Remove:
                        if (nextModelId is not 1)
                            operations.Add(new RemoveOperation()
                            {
                                ModelId = rng.Next(maxValue: nextModelId)
                            });
                        break;

                    case OperationType.Replace:
                        if (nextModelId is not 1)
                        {
                            var replacementKey = rng.Next(maxValue: nextModelId);
                            operations.Add(new ReplaceOperation()
                            {
                                Model = new()
                                {
                                    Id      = replacementKey,
                                    Value   = rng.Next(maxValue: MaxDataValue + 1)
                                }
                            });
                        }
                        break;

                    case OperationType.Reset:
                        {
                            nextModelId = 1;
                            
                            var modelsCount = rng.Next(minValue: 1, maxValue: MaxChangeSetSize + 1);
                            var models = new List<DataModel>(capacity: modelsCount);

                            while(models.Count < modelsCount)
                                models.Add(new()
                                {
                                    Id      = nextModelId++,
                                    Value   = rng.Next(maxValue: MaxDataValue + 1)
                                });

                            operations.Add(new ResetOperation()
                            {
                                Models = models
                            });
                        }
                        break;
                }
            }

            _operations = operations;
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _destination    = new();
        }

        [Benchmark]
        public void ConsumerInterpretedChanges()
        {
            using var models = new ConsumerInterpretedChanges.ObservableDictionary<int, DataModel>();

            {
                using var subscription = models
                    .WhereItems(static dataModel => dataModel.Value <= ThresholdDataValue)
                    .OrderItems(DataModel.Comparer)
                    .SelectItems(static dataModel => dataModel.Id)
                    .SelectAndCacheItems(dataModelId => new DataViewModel(models.ObserveValue(dataModelId)))
                    .DisposeItemsAfterRemoval()
                    .Subscribe(changeSet => changeSet.ApplyTo(_destination));

                foreach(var operation in _operations)
                    operation.ApplyTo(models);
            }

            foreach(var viewModel in _destination)
                viewModel.Dispose();
        }

        [Benchmark(Baseline = true)]
        public void DynamicDataChangeSets()
        {
            using var models = new DynamicDataChangeSets.ObservableDictionary<int, DataModel>();

            {
                using var subscription = models
                    .WhereItems(static dataModel => dataModel.Value <= ThresholdDataValue)
                    .OrderItems(DataModel.Comparer)
                    .SelectItems(static dataModel => dataModel.Id)
                    .SelectAndCacheItems(dataModelId => new DataViewModel(models.ObserveValue(dataModelId)))
                    .DisposeItemsAfterRemoval()
                    .Subscribe(changeSet => changeSet.ApplyTo(_destination));

                foreach(var operation in _operations)
                    operation.ApplyTo(models);
            }

            foreach(var viewModel in _destination)
                viewModel.Dispose();
        }

        [Benchmark]
        public void ImmutableArrayChanges()
        {
            using var models = new ImmutableArrayChanges.ObservableDictionary<int, DataModel>();

            {
                using var subscription = models
                    .WhereItems(static dataModel => dataModel.Value <= ThresholdDataValue)
                    .OrderItems(DataModel.Comparer)
                    .SelectItems(static dataModel => dataModel.Id)
                    .SelectAndCacheItems(dataModelId => new DataViewModel(models.ObserveValue(dataModelId)))
                    .DisposeItemsAfterRemoval()
                    .Subscribe(changeSet => changeSet.ApplyTo(_destination));

                foreach(var operation in _operations)
                    operation.ApplyTo(models);
            }

            foreach(var viewModel in _destination)
                viewModel.Dispose();
        }

        [Benchmark]
        public void IReadOnlyListChanges()
        {
            using var models = new IReadOnlyListChanges.ObservableDictionary<int, DataModel>();

            {
                using var subscription = models
                    .WhereItems(static dataModel => dataModel.Value <= ThresholdDataValue)
                    .OrderItems(DataModel.Comparer)
                    .SelectItems(static dataModel => dataModel.Id)
                    .SelectAndCacheItems(dataModelId => new DataViewModel(models.ObserveValue(dataModelId)))
                    .DisposeItemsAfterRemoval()
                    .Subscribe(changeSet => changeSet.ApplyTo(_destination));

                foreach(var operation in _operations)
                    operation.ApplyTo(models);
            }

            foreach(var viewModel in _destination)
                viewModel.Dispose();
        }

        [Benchmark]
        public void PolymorphicChangeSets()
        {
            using var models = new PolymorphicChangeSets.ObservableDictionary<int, DataModel>();

            {
                using var subscription = models
                    .WhereItems(static dataModel => dataModel.Value <= ThresholdDataValue)
                    .OrderItems(DataModel.Comparer)
                    .SelectItems(static dataModel => dataModel.Id)
                    .SelectAndCacheItems(dataModelId => new DataViewModel(models.ObserveValue(dataModelId)))
                    .DisposeItemsAfterRemoval()
                    .Subscribe(changeSet => changeSet.ApplyTo(_destination));

                foreach(var operation in _operations)
                    operation.ApplyTo(models);
            }

            foreach(var viewModel in _destination)
                viewModel.Dispose();
        }

        private static readonly IReadOnlyList<OperationType> _operationTypes
            = Enum.GetValues<OperationType>();

        private readonly IReadOnlyList<OperationBase> _operations;

        private List<DataViewModel> _destination = null!;


        public enum OperationType
        {
            Add,
            AddRange,
            Clear,
            Remove,
            Replace,
            Reset
        }

        public class DataModel
        {
            public static readonly IComparer<DataModel> Comparer
                = Comparer<DataModel>.Create((x, y) => x.Value.CompareTo(y.Value));

            public required int Id { get; init; }

            public required int Value { get; init; }
        }

        public sealed class DataViewModel
            : DisposableBase
        {
            public DataViewModel(IObservable<DataModel> dataModel)
                => _subscription = dataModel.Subscribe(dataModel =>
                {
                    _id     = dataModel.Id;
                    _value  = dataModel.Value.ToString();
                });

            public int Id
                => _id;

            public string? Value
                => _value;

            protected override void OnDisposing(DisposalType type)
            {
                if (type is DisposalType.Managed)
                    _subscription.Dispose();
            }

            private readonly IDisposable _subscription;

            private int     _id;
            private string? _value;
        }

        public abstract class OperationBase
        {
            public abstract void ApplyTo(ConsumerInterpretedChanges.ObservableDictionary<int, DataModel> models);

            public abstract void ApplyTo(DynamicDataChangeSets.ObservableDictionary<int, DataModel> models);

            public abstract void ApplyTo(ImmutableArrayChanges.ObservableDictionary<int, DataModel> models);

            public abstract void ApplyTo(IReadOnlyListChanges.ObservableDictionary<int, DataModel> models);
            
            public abstract void ApplyTo(PolymorphicChangeSets.ObservableDictionary<int, DataModel> models);
        }

        public sealed class AddOperation
            : OperationBase
        {
            public required DataModel Model { get; init; }

            public override void ApplyTo(ConsumerInterpretedChanges.ObservableDictionary<int, DataModel> models)
                => models.Add(
                    key:    Model.Id,
                    value:  Model);

            public override void ApplyTo(DynamicDataChangeSets.ObservableDictionary<int, DataModel> models)
                => models.Add(
                    key:    Model.Id,
                    value:  Model);

            public override void ApplyTo(ImmutableArrayChanges.ObservableDictionary<int, DataModel> models)
                => models.Add(
                    key:    Model.Id,
                    value:  Model);

            public override void ApplyTo(IReadOnlyListChanges.ObservableDictionary<int, DataModel> models)
                => models.Add(
                    key:    Model.Id,
                    value:  Model);

            public override void ApplyTo(PolymorphicChangeSets.ObservableDictionary<int, DataModel> models)
                => models.Add(
                    key:    Model.Id,
                    value:  Model);
        }

        public sealed class AddRangeOperation
            : OperationBase
        {
            public required IEnumerable<DataModel> Models { get; init; }

            public override void ApplyTo(ConsumerInterpretedChanges.ObservableDictionary<int, DataModel> models)
                => models.AddRange(
                    values:         Models,
                    keySelector:    static model => model.Id);

            public override void ApplyTo(DynamicDataChangeSets.ObservableDictionary<int, DataModel> models)
                => models.AddRange(
                    values:         Models,
                    keySelector:    static model => model.Id);

            public override void ApplyTo(ImmutableArrayChanges.ObservableDictionary<int, DataModel> models)
                => models.AddRange(
                    values:         Models,
                    keySelector:    static model => model.Id);

            public override void ApplyTo(IReadOnlyListChanges.ObservableDictionary<int, DataModel> models)
                => models.AddRange(
                    values:         Models,
                    keySelector:    static model => model.Id);

            public override void ApplyTo(PolymorphicChangeSets.ObservableDictionary<int, DataModel> models)
                => models.AddRange(
                    values:         Models,
                    keySelector:    static model => model.Id);
        }

        public sealed class ClearOperation
            : OperationBase
        {
            public override void ApplyTo(ConsumerInterpretedChanges.ObservableDictionary<int, DataModel> models)
                => models.Clear();

            public override void ApplyTo(DynamicDataChangeSets.ObservableDictionary<int, DataModel> models)
                => models.Clear();

            public override void ApplyTo(ImmutableArrayChanges.ObservableDictionary<int, DataModel> models)
                => models.Clear();

            public override void ApplyTo(IReadOnlyListChanges.ObservableDictionary<int, DataModel> models)
                => models.Clear();

            public override void ApplyTo(PolymorphicChangeSets.ObservableDictionary<int, DataModel> models)
                => models.Clear();
        }

        public sealed class RemoveOperation
            : OperationBase
        {
            public required int ModelId { get; init; }

            public override void ApplyTo(ConsumerInterpretedChanges.ObservableDictionary<int, DataModel> models)
                => models.Remove(ModelId);

            public override void ApplyTo(DynamicDataChangeSets.ObservableDictionary<int, DataModel> models)
                => models.Remove(ModelId);

            public override void ApplyTo(ImmutableArrayChanges.ObservableDictionary<int, DataModel> models)
                => models.Remove(ModelId);

            public override void ApplyTo(IReadOnlyListChanges.ObservableDictionary<int, DataModel> models)
                => models.Remove(ModelId);

            public override void ApplyTo(PolymorphicChangeSets.ObservableDictionary<int, DataModel> models)
                => models.Remove(ModelId);
        }

        public sealed class ReplaceOperation
            : OperationBase
        {
            public required DataModel Model { get; init; }

            public override void ApplyTo(ConsumerInterpretedChanges.ObservableDictionary<int, DataModel> models)
                => models[Model.Id] = Model;

            public override void ApplyTo(DynamicDataChangeSets.ObservableDictionary<int, DataModel> models)
                => models[Model.Id] = Model;

            public override void ApplyTo(ImmutableArrayChanges.ObservableDictionary<int, DataModel> models)
                => models[Model.Id] = Model;

            public override void ApplyTo(IReadOnlyListChanges.ObservableDictionary<int, DataModel> models)
                => models[Model.Id] = Model;

            public override void ApplyTo(PolymorphicChangeSets.ObservableDictionary<int, DataModel> models)
                => models[Model.Id] = Model;
        }

        public sealed class ResetOperation
            : OperationBase
        {
            public required IEnumerable<DataModel> Models { get; init; }

            public override void ApplyTo(ConsumerInterpretedChanges.ObservableDictionary<int, DataModel> models)
                => models.Reset(
                    values:         Models,
                    keySelector:    static model => model.Id);

            public override void ApplyTo(DynamicDataChangeSets.ObservableDictionary<int, DataModel> models)
                => models.Reset(
                    values:         Models,
                    keySelector:    static model => model.Id);

            public override void ApplyTo(ImmutableArrayChanges.ObservableDictionary<int, DataModel> models)
                => models.Reset(
                    values:         Models,
                    keySelector:    static model => model.Id);

            public override void ApplyTo(IReadOnlyListChanges.ObservableDictionary<int, DataModel> models)
                => models.Reset(
                    values:         Models,
                    keySelector:    static model => model.Id);

            public override void ApplyTo(PolymorphicChangeSets.ObservableDictionary<int, DataModel> models)
                => models.Reset(
                    values:         Models,
                    keySelector:    static model => model.Id);
        }
    }
}
