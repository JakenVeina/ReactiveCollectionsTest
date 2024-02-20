namespace ReactiveCollectionsTest.PolymorphicChangeSets
{
    public readonly record struct SortedUpdate<T>
    {
        public required int NewIndex { get; init; }

        public required T NewItem { get; init; }
        
        public required int OldIndex { get; init; }

        public required T OldItem { get; init; }
    }
}
