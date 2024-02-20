using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace ReactiveCollectionsTest
{
    public class EntryPoint
    {
        public static void Main(string[] args)
        {
            BenchmarkSwitcher
                .FromAssembly(Assembly.GetEntryAssembly()!)
                .Run(args, DefaultConfig.Instance
                    .WithArtifactsPath(Path.Combine(
                        GetProjectRootDirectory(),
                        Path.GetFileName(DefaultConfig.Instance.ArtifactsPath))));


            //var benchmark = new Benchmarks.ReactiveCollectionItemsQuery();
            //benchmark.IterationSetup();

            //benchmark.DynamicDataChangeSets();


            // TODO: Benchmark List<T>.Sort() versus FindInsertionIndex(), for processing Reset changesets in .OrderItems()
            // TODO: Benchmark ImmutableArray<T> versus IReadOnlyList<T> within changesets
        }

        private static string GetProjectRootDirectory([CallerFilePath] string? callerFilePath = null)
            => Path.GetDirectoryName(callerFilePath)!;
    }
}