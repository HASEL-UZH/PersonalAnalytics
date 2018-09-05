using System;
using System.Drawing;
using System.Reactive.Linq;
using EyeCatcherLib.Native;

namespace EyeCatcherLib.Providers
{
    // ReSharper disable once ClassNeverInstantiated.Global - Justification IOC
    public class CursorPointProvider : DisposableBase, IObservable<PointTime>
    {
        private readonly IObservable<PointTime> _internalObservable;

        public CursorPointProvider()
        {
            _internalObservable = Observable.Interval(TimeSpan.FromSeconds(1)).Select(lng =>
            {
                NativeMethods.GetCursorPos(out var point);
                return new PointTime((Point)point);
            });
        }

        public IDisposable Subscribe(IObserver<PointTime> observer)
        {
            return _internalObservable.Subscribe(observer);
        }
    }
}
