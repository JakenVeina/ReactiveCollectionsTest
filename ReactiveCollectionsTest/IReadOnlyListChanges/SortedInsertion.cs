namespace ReactiveCollectionsTest.IReadOnlyListChanges
{
    public readonly record struct SortedInsertion<T>
    {
        public required int Index { get; init; }

        public required T Item { get; init; }
    }
}
