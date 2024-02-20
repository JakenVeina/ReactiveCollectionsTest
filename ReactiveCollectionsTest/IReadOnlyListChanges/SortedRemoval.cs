namespace ReactiveCollectionsTest.IReadOnlyListChanges
{
    public readonly record struct SortedRemoval<T>
    {
        public required int Index { get; init; }

        public required T Item { get; init; }
    }
}
