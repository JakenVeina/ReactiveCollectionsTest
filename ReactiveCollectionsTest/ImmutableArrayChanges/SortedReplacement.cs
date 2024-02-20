namespace ReactiveCollectionsTest.ImmutableArrayChanges
{
    public readonly record struct SortedReplacement<T>
    {
        public required int Index { get; init; }

        public required T NewItem { get; init; }
        
        public required T OldItem { get; init; }
    }
}
