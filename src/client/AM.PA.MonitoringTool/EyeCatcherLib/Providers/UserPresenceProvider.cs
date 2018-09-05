using System;
using System.Reactive.Linq;
using EyeCatcherDatabase.Records;
using Tobii.Interaction;
using Tobii.Interaction.Framework;

namespace EyeCatcherLib.Providers
{
    public class UserPresenceProvider : EyeTrackingProvider, IObservable<UserPresenceRecord>
    {
        private readonly IObservable<UserPresenceRecord> _userPresenceStream;

        public UserPresenceProvider()
        {
            var userPresenceObserver = Host.States.CreateUserPresenceObserver();
            _userPresenceStream = Observable.FromEventPattern<EngineStateValue<UserPresence>>(
                    h => userPresenceObserver.Changed += h,
                    h => userPresenceObserver.Changed -= h)
                .Select(x => new UserPresenceRecord { UserPresence = (EyeCatcherDatabase.Enums.UserPresence)x.EventArgs.Value });
        }

        public IDisposable Subscribe(IObserver<UserPresenceRecord> observer)
        {
            return _userPresenceStream.Subscribe(observer);
        }

    }
}
