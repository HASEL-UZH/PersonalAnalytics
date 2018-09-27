using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Forms;
using EyeCatcher.Clipboard;
using EyeCatcherDatabase;
using EyeCatcherDatabase.Enums;
using EyeCatcherDatabase.Records;
using EyeCatcherLib;
using EyeCatcherLib.Providers;
using Microsoft.Win32;

namespace EyeCatcher.DataCollection
{
    /// <summary>
    /// Uses WinForms to get information about windows and screens
    /// </summary>
    internal class DataCollector : IDisposable
    {
        private readonly WindowManager _windowManager;
        private readonly IWriteAsyncDatabase _database;
        private readonly IScreenLayoutRecordProvider _screenLayoutRecordProvider;
        private readonly IObservable<PointTime> _fixationPointProvider;
        private readonly HeadPoseProvider _headPoseProvider;
        private readonly IObservable<PointTime> _cursorPointProvider;
        private readonly LastUserInputTicksProvider _userInputProvider;
        private readonly IObservable<UserPresenceRecord> _userPresence;
        private readonly DesktopPointRecordProvider _desktopPointRecordProvider;
        private readonly ClipboardMonitor _clipboardMonitor;
        private readonly EyePositionProvider _eyePositionProvider;

        private readonly ConcurrentQueue<DesktopPointRecord> _desktopPointQueue = new ConcurrentQueue<DesktopPointRecord>();
        private readonly ConcurrentQueue<LastUserInputRecord> _lastUserInputQueue = new ConcurrentQueue<LastUserInputRecord>();
        private readonly ConcurrentQueue<HeadPoseRecord> _headPoseQueue = new ConcurrentQueue<HeadPoseRecord>();
        private readonly ConcurrentQueue<EyePositionRecord> _eyePositionQueue = new ConcurrentQueue<EyePositionRecord>();

        private readonly IList<IDisposable> _disposables = new List<IDisposable>();
        private Timer _timer;


        public DataCollector(WindowManager windowManager,
            IWriteAsyncDatabase database,
            IScreenLayoutRecordProvider screenLayoutRecordProvider,
            FixationPointProvider fixationPointProvider,
            HeadPoseProvider headPoseProvider,
            EyePositionProvider eyePositionProvider,
            CursorPointProvider cursorPointProvider,
            LastUserInputTicksProvider userInputProvider,
            IObservable<UserPresenceRecord> userPresence)
        {
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
            _database = database;
            _screenLayoutRecordProvider = screenLayoutRecordProvider ?? throw new ArgumentNullException(nameof(screenLayoutRecordProvider));
            _fixationPointProvider = fixationPointProvider ?? throw new ArgumentNullException(nameof(fixationPointProvider));
            _headPoseProvider = headPoseProvider ?? throw new ArgumentNullException(nameof(headPoseProvider));
            _eyePositionProvider = eyePositionProvider ?? throw new ArgumentNullException(nameof(eyePositionProvider));
            _cursorPointProvider = cursorPointProvider ?? throw new ArgumentNullException(nameof(cursorPointProvider));
            _userInputProvider = userInputProvider ?? throw new ArgumentNullException(nameof(userInputProvider));
            _userPresence = userPresence ?? throw new ArgumentNullException(nameof(userPresence));
            _desktopPointRecordProvider = new DesktopPointRecordProvider(_windowManager.Windows);
            _clipboardMonitor = new ClipboardMonitor(_windowManager.Windows);
        }

        public void Start()
        {
            // Inserting current screen layout
            _database.InsertAsync(_screenLayoutRecordProvider.GetScreenLayout());

            // Collecting the Screen Layout
            // TODO RR: Clean and unsubscribe
            SystemEvents.DisplaySettingsChanged += async (s, e) =>
            {
                // TODO RR: all Windows must be updated in the WindowManager now ... (they will probably move/activate - but needs checking)
                await _database.InsertAsync(_screenLayoutRecordProvider.GetScreenLayout());
            };

            _windowManager.Start();

            // Collecting Information about points in the environment
            _disposables.Add(_fixationPointProvider.Subscribe(Observer.Create<PointTime>(
                point =>
                {
                    try
                    {
                        var desktoPoint = _desktopPointRecordProvider.GetDesktopPointInfo(point, DesktopPointType.Fixation);
                        _desktopPointQueue.Enqueue(desktoPoint);
                    }
                    catch (Exception e)
                    {
                        // ignore
                    }
                },
                exception => Debug.WriteLine(exception.Message))));

            _disposables.Add(_cursorPointProvider.Subscribe(Observer.Create<PointTime>(
                point =>
                {
                    try
                    {
                        var desktoPoint = _desktopPointRecordProvider.GetDesktopPointInfo(point, DesktopPointType.MousePosition);
                        _desktopPointQueue.Enqueue(desktoPoint);
                    }
                    catch (Exception e)
                    {
                        // ignore
                    }
                },
                exception => Debug.WriteLine(exception.Message))));

            _disposables.Add(_headPoseProvider.Subscribe(Observer.Create<HeadPoseRecord>(
                pose => _headPoseQueue.Enqueue(pose),
                exception => Debug.WriteLine(exception.Message))));

            _disposables.Add(_eyePositionProvider.Subscribe(Observer.Create<EyePositionRecord>(
                eyePosition => _eyePositionQueue.Enqueue(eyePosition),
                exception => Debug.WriteLine(exception.Message))));

            _disposables.Add(_userInputProvider.Subscribe(Observer.Create<uint>(
                ticks => _lastUserInputQueue.Enqueue(new LastUserInputRecord { Ticks = ticks }),
                exception => Debug.WriteLine(exception.Message))));

            // for Performance inserting bulk every 20 seconds
            _timer = new Timer { Interval = 20000 };
            _timer.Tick += async (sender, args) => { await StoreQueuesInDatabase(); };
            _timer.Start();

            // Collecting Information the environment
            var desktopObserver = Observer.Create<DesktopRecord>(async desktopRecord =>
            {
                await _database.InsertOrReplaceAsync(desktopRecord);
            }, exception => Debug.WriteLine(exception.Message));
            _disposables.Add(_windowManager.Windows.Subscribe(desktopObserver));

            // CopyPaste Information
            var copyPasteObserver = Observer.Create<CopyPasteRecord>(async copyPasteRecord =>
            {
                await _database.InsertAsync(copyPasteRecord);
            }, exception => Debug.WriteLine(exception.Message));
            _disposables.Add(_clipboardMonitor.Subscribe(copyPasteObserver));

            // User Information
            var userPresenceObserver = Observer.Create<UserPresenceRecord>(async userPresenceRecord =>
            {
                await _database.InsertAsync(userPresenceRecord);
            }, exception => Debug.WriteLine(exception.Message));
            _disposables.Add(_userPresence.Subscribe(userPresenceObserver));
        }

        private async Task StoreQueuesInDatabase()
        {
            await InsertQueueAsync(_desktopPointQueue);
            await InsertQueueAsync(_lastUserInputQueue);
            await InsertQueueAsync(_headPoseQueue);
            await InsertQueueAsync(_eyePositionQueue);
        }

        private async Task InsertQueueAsync<T>(ConcurrentQueue<T> queue) where T : Record
        {
            if (queue.IsEmpty)
            {
                return;
            }

            var eyePositionList = new List<T>();
            while (queue.TryDequeue(out var eyePositionRecord))
            {
                eyePositionList.Add(eyePositionRecord);
            }
            await _database.InsertAllAsync(eyePositionList);
        }

        public void Dispose()
        {
            // TODO RR: Check how to empty queues?
            _timer?.Stop();
            _timer?.Dispose();

            foreach (var disposable in _disposables)
            {
                disposable?.Dispose();
            }
        }
    }
}
