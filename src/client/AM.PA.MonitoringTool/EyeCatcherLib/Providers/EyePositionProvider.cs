using System;
using System.Reactive.Linq;
using EyeCatcherDatabase.Records;
using Tobii.Interaction;

namespace EyeCatcherLib.Providers
{
    // ReSharper disable once ClassNeverInstantiated.Global - Justification IOC
    public class EyePositionProvider : EyeTrackingProvider, IObservable<EyePositionRecord>
    {
        private readonly IObservable<EyePositionRecord> _internalHeadPoseStream;

        public EyePositionProvider()
        {
            _internalHeadPoseStream = Host.Streams.CreateEyePositionStream()
                .Sample(TimeSpan.FromMilliseconds(500))
                .Select(PointTimeFromFixation);
        }

        private static EyePositionRecord PointTimeFromFixation(StreamData<EyePositionData> eyePositionData)
        {
            return new EyePositionRecord
            {
                LeftEyeX  = eyePositionData.Data.LeftEye.X,
                LeftEyeY  = eyePositionData.Data.LeftEye.Y,
                LeftEyeZ  = eyePositionData.Data.LeftEye.Z,
                LeftEyeNormalizedX = eyePositionData.Data.LeftEyeNormalized.X,
                LeftEyeNormalizedY = eyePositionData.Data.LeftEyeNormalized.Y,
                LeftEyeNormalizedZ = eyePositionData.Data.LeftEyeNormalized.Z,
                RightEyeX = eyePositionData.Data.RightEye.X,
                RightEyeY = eyePositionData.Data.RightEye.Y,
                RightEyeZ = eyePositionData.Data.RightEye.Z,
                RightEyeNormalizedX = eyePositionData.Data.RightEyeNormalized.X,
                RightEyeNormalizedY = eyePositionData.Data.RightEyeNormalized.Y,
                RightEyeNormalizedZ = eyePositionData.Data.RightEyeNormalized.Z,
            };
        }

        public IDisposable Subscribe(IObserver<EyePositionRecord> observer)
        {
            return _internalHeadPoseStream.Subscribe(observer);
        }
    }
}
