namespace ReactiveCollectionsTest.PolymorphicChangeSets
{
    public readonly record struct KeyedRemoval<TKey, TItem>
    {
        public required TItem Item { get; init; }

        public required TKey Key { get; init; }
    }
}
