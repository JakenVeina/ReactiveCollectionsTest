using System;

namespace ReactiveCollectionsTest
{
    public abstract class DisposableBase
        : IDisposable
    {
        ~DisposableBase()
            => OnDisposing(DisposalType.Unmanaged);

        public void Dispose()
        {
            if (!_hasDisposed)
            {
                OnDisposing(DisposalType.Managed);
                GC.SuppressFinalize(this);
                _hasDisposed = true;
            }
        }

        protected abstract void OnDisposing(DisposalType type);

        private bool _hasDisposed;
    }
}
