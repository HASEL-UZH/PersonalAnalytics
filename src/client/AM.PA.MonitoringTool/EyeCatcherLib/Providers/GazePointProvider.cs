using System;
using System.Reactive.Linq;

namespace EyeCatcherLib.Providers
{
    // ReSharper disable once ClassNeverInstantiated.Global - Justification IOC
    public class GazePointProvider : EyeTrackingProvider, IObservable<PointTime>
    {
        private readonly IObservable<PointTime> _internalGazePointDataStream;

        public GazePointProvider()
        {
            _internalGazePointDataStream = Host.Streams.CreateGazePointDataStream()
                .Where(f => f.Data.X > int.MinValue && f.Data.Y > int.MinValue)
                .Select(streamData => new PointTime((int) streamData.Data.X, (int) streamData.Data.Y));
        }

        public IDisposable Subscribe(IObserver<PointTime> observer)
        {
            return _internalGazePointDataStream.Subscribe(observer);
        }
    }
}
