using System;

namespace ReactiveCollectionsTest
{
    public readonly record struct Optional<T>
    {
        public static readonly Optional<T> Unspecified
            = new(false, default!);

        private Optional(bool isSpecified, T value)
        {
            _isSpecified    = isSpecified;
            _value          = value;
        }

        public bool IsSpecified
            => _isSpecified;

        public T Value
            => _isSpecified
                ? _value
                : throw new InvalidOperationException($"Cannot retrieve {nameof(Value)} from an unspecified {nameof(Optional<T>)}");

        public T Unwrap(T fallbackValue)
            => _isSpecified
                ? _value
                : fallbackValue;

        public static implicit operator Optional<T>(T value)
            => new(true, value);

        private readonly bool   _isSpecified;
        private readonly T      _value;
    }
}
