namespace ReactiveCollectionsTest.ImmutableArrayChanges
{
    public readonly record struct KeyedReplacement<TKey, TItem>
    {
        public required TKey Key { get; init; }

        public required TItem NewItem { get; init; }
        
        public required TItem OldItem { get; init; }
    }
}
