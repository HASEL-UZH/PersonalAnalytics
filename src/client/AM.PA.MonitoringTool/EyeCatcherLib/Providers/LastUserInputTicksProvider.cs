using System;
using System.Reactive.Linq;
using EyeCatcherLib.Native;

namespace EyeCatcherLib.Providers
{
    // ReSharper disable once ClassNeverInstantiated.Global - Justification IOC
    public class LastUserInputTicksProvider : DisposableBase, IObservable<uint>
    {
        private readonly IObservable<uint> _internalObservable;

        public LastUserInputTicksProvider()
        {
            _internalObservable = Observable.Interval(TimeSpan.FromSeconds(1))
                .Select(lng => NativeMethods.GetLastInputTick());
        }

        public IDisposable Subscribe(IObserver<uint> observer)
        {
            return _internalObservable.Subscribe(observer);
        }
    }
}