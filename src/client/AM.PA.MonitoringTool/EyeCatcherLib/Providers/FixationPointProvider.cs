using System;
using System.Reactive.Linq;
using Tobii.Interaction;

namespace EyeCatcherLib.Providers
{
    // ReSharper disable once ClassNeverInstantiated.Global - Justification IOC
    public class FixationPointProvider : EyeTrackingProvider, IObservable<PointTime>
    {
        private readonly IObservable<PointTime> _internalGazePointDataStream;

        public FixationPointProvider()
        {
            _internalGazePointDataStream = Host.Streams.CreateFixationDataStream()
                .Where(f => f.Data.X > int.MinValue && f.Data.Y > int.MinValue)
                .Select(PointTimeFromFixation);
        }

        private static PointTime PointTimeFromFixation(StreamData<FixationData> fixation)
        {
            return new FixationPointTime((int) fixation.Data.X, (int) fixation.Data.Y, fixation.Data.EventType.ToString());
        }

        public IDisposable Subscribe(IObserver<PointTime> observer)
        {
            return _internalGazePointDataStream.Subscribe(observer);
        }

    }
}
