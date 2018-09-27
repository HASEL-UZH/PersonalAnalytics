using System;
using System.Reactive.Linq;
using EyeCatcherDatabase.Records;
using Tobii.Interaction;

namespace EyeCatcherLib.Providers
{
    // ReSharper disable once ClassNeverInstantiated.Global - Justification IOC
    public class HeadPoseProvider : EyeTrackingProvider, IObservable<HeadPoseRecord>
    {
        private readonly IObservable<HeadPoseRecord> _internalHeadPoseStream;

        public HeadPoseProvider()
        {
            _internalHeadPoseStream = Host.Streams.CreateHeadPoseStream()
                .Where(data => data.Data.HasHeadPosition)
                .Sample(TimeSpan.FromMilliseconds(500))
                .Select(PointTimeFromFixation);
        }

        private static HeadPoseRecord PointTimeFromFixation(StreamData<HeadPoseData> headPoseData)
        {
            return new HeadPoseRecord
            {
                HeadPositionX = headPoseData.Data.HeadPosition.X,
                HeadPositionY = headPoseData.Data.HeadPosition.Y,
                HeadPositionZ = headPoseData.Data.HeadPosition.Z,
                HeadRotationX = headPoseData.Data.HeadRotation.X,
                HeadRotationY = headPoseData.Data.HeadPosition.Y,
                HeadRotationZ = headPoseData.Data.HeadPosition.Z,
            };
        }

        public IDisposable Subscribe(IObserver<HeadPoseRecord> observer)
        {
            return _internalHeadPoseStream.Subscribe(observer);
        }
    }
}
