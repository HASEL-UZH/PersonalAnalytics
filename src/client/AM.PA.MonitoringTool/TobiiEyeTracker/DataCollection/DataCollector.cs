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
        private readonly IObservable<PointTime> _cursorPointProvider;
        private readonly LastUserInputTicksProvider _userInputProvider;
        private readonly IObservable<UserPresenceRecord> _userPresence;
        private readonly DesktopPointRecordProvider _desktopPointRecordProvider;
        private readonly ClipboardMonitor _clipboardMonitor;

        private readonly ConcurrentQueue<DesktopPointRecord> _desktopPointQueue = new ConcurrentQueue<DesktopPointRecord>();
        private readonly ConcurrentQueue<LastUserInputRecord> _lastUserInputQueue = new ConcurrentQueue<LastUserInputRecord>();

        private IDisposable _fixationPointSubscription;
        private IDisposable _desktopSubscription;
        private IDisposable _copyPasteSubscription;
        private IDisposable _userPresenceSubscription;
        private IDisposable _cursorPointSubscription;
        private IDisposable _lastUserInputSubscription;
        private Timer _timer;

        public DataCollector(WindowManager windowManager,
            IWriteAsyncDatabase database,
            IScreenLayoutRecordProvider screenLayoutRecordProvider,
            FixationPointProvider fixationPointProvider,
            CursorPointProvider cursorPointProvider,
            LastUserInputTicksProvider userInputProvider,
            IObservable<UserPresenceRecord> userPresence)
        {
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
            _database = database;
            _screenLayoutRecordProvider = screenLayoutRecordProvider ?? throw new ArgumentNullException(nameof(screenLayoutRecordProvider));
            _fixationPointProvider = fixationPointProvider ?? throw new ArgumentNullException(nameof(fixationPointProvider));
            _cursorPointProvider = cursorPointProvider ?? throw new ArgumentNullException(nameof(cursorPointProvider));
            _userInputProvider = userInputProvider ?? throw new ArgumentNullException(nameof(userInputProvider));
            _userPresence = userPresence ?? throw new ArgumentNullException(nameof(userPresence));
            _desktopPointRecordProvider = new DesktopPointRecordProvider(_windowManager.Windows);
            _clipboardMonitor = new ClipboardMonitor(_windowManager.Windows);
        }

        public bool IsDisposed { get; set; }

        public void Start()
        {
            // Inserting current screen layout
            _database.InsertAsync(_screenLayoutRecordProvider.GetScreenLayout());

            // Collecting the Screen Layout
            // TODO RR: Clean and unsubscribe
            SystemEvents.DisplaySettingsChanged += async (s,e) =>
            {
                // TODO RR: all Windows must be updated in the WindowManager now ... (they will probably move/activate - but needs checking)
                await _database.InsertAsync(_screenLayoutRecordProvider.GetScreenLayout());
            };

            _windowManager.Start();

            // Collecting Information about points in the environment
            _fixationPointSubscription = _fixationPointProvider.Subscribe(Observer.Create<PointTime>(
                point => _desktopPointQueue.Enqueue(_desktopPointRecordProvider.GetDesktopPointInfo(point, DesktopPointType.Fixation)),
                exception => Debug.WriteLine(exception)));

            _cursorPointSubscription = _cursorPointProvider.Subscribe(Observer.Create<PointTime>(
                point => _desktopPointQueue.Enqueue(_desktopPointRecordProvider.GetDesktopPointInfo(point, DesktopPointType.MousePosition)),
                exception => Debug.WriteLine(exception)));

            _lastUserInputSubscription = _userInputProvider.Subscribe(Observer.Create<uint>(
                ticks => _lastUserInputQueue.Enqueue(new LastUserInputRecord{ Ticks = ticks }),
                exception => Debug.WriteLine(exception)));

            // for Performance inserting bulk every 20 seconds
            _timer = new Timer { Interval = 20000 };
            _timer.Tick += async (sender, args) => { await StoreQueuesInDatabase(); };
            _timer.Start();

            // Collecting Information the environment
            var desktopObserver = Observer.Create<DesktopRecord>(async desktopRecord =>
            {
                await _database.InsertOrReplaceAsync(desktopRecord);
            }, exception => Debug.WriteLine(exception));
            _desktopSubscription = _windowManager.Windows.Subscribe(desktopObserver);

            // CopyPaste Information
            var copyPasteObserver = Observer.Create<CopyPasteRecord>(async copyPasteRecord =>
            {
                await _database.InsertAsync(copyPasteRecord);
            }, exception => Debug.WriteLine(exception));
            _copyPasteSubscription = _clipboardMonitor.Subscribe(copyPasteObserver);

            // User Information
            var userPresenceObserver = Observer.Create<UserPresenceRecord>(async userPresenceRecord =>
            {
                await _database.InsertAsync(userPresenceRecord);
            }, exception => Debug.WriteLine(exception));
            _userPresenceSubscription = _userPresence.Subscribe(userPresenceObserver);
        }

        private async Task StoreQueuesInDatabase()
        {
            // DesktopPoints
            var pointList = new List<DesktopPointRecord>();
            while (_desktopPointQueue.TryDequeue(out var desktopPointRecord))
            {
                pointList.Add(desktopPointRecord);
            }
            await _database.InsertAllAsync(pointList);

            var ticksList = new List<LastUserInputRecord>();
            while (_lastUserInputQueue.TryDequeue(out var lastUserInputRecord))
            {
                ticksList.Add(lastUserInputRecord);
            }
            await _database.InsertAllAsync(ticksList);
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            IsDisposed = true;
            // TODO RR: Check how to empty queues?
            _timer?.Stop();
            _timer?.Dispose();
            _fixationPointSubscription?.Dispose();
            _cursorPointSubscription?.Dispose();
            _lastUserInputSubscription?.Dispose();
            _desktopSubscription?.Dispose();
            _copyPasteSubscription?.Dispose();
            _userPresenceSubscription?.Dispose();
            _clipboardMonitor?.Dispose();
        }

    }
}
