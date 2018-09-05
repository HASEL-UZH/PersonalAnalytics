using Tobii.Interaction;

namespace EyeCatcherLib.Providers
{
    public abstract class EyeTrackingProvider : DisposableBase
    {
        protected readonly Host Host = new Host();

        protected override void Dispose(bool disposing)
        {
            if (Disposed)
            {
                return;
            }

            if (disposing)
            {
                // Free other state (managed objects).
            }
            // Free your own state (unmanaged objects).
            Host?.Dispose();

            base.Dispose(disposing);
        }

    }
}
