using System.Collections.Immutable;

namespace ReactiveCollectionsTest
{
    public static class ImmutableArrayBuilderExtensions
    {
        public static ImmutableArray<T> MoveToOrCreateImmutable<T>(this ImmutableArray<T>.Builder builder)
            => (builder.Count == builder.Capacity)
                ? builder.MoveToImmutable()
                : builder.ToImmutable();
    }
}
