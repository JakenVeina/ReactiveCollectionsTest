namespace ReactiveCollectionsTest.ImmutableArrayChanges
{
    public readonly record struct SortedMovement<T>
    {
        public required T Item { get; init; }

        public required int NewIndex { get; init; }

        public required int OldIndex { get; init; }
    }
}
