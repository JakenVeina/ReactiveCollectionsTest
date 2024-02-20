namespace ReactiveCollectionsTest.IReadOnlyListChanges
{
    public readonly record struct KeyedAddition<TKey, TItem>
    {
        public required TItem Item { get; init; }

        public required TKey Key { get; init; }
    }
}
