using System;

namespace EyeCatcherLib.Providers
{
    public abstract class DisposableBase : IDisposable
    {
        #region IDisposable

        protected bool Disposed { get; private set; }

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed)
            {
                return;
            }

            Disposed = true;
            if (disposing)
            {
                // Free other state (managed objects).
            }
            // Free your own state (unmanaged objects).
        }

        public void Dispose()
        {
            Dispose(true);
        }

        ~DisposableBase()
        {
            Dispose(false);
        }

        #endregion
    }
}
