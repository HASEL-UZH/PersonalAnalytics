// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;
using Shared;
using UserInputTracker.Data;
using UserInputTracker.Models;
using Timer = System.Timers.Timer;
using UserInputTracker.Visualizations;
using Shared.Data;

namespace UserInputTracker
{
    public sealed class Daemon : BaseTrackerDisposable, ITracker
    {
        #region FIELDS

        private bool _disposed = false;
        private IKeyboardMouseEvents _mEvents;
        //private Timer _mouseSnapshotTimer;
        private Timer _saveToDatabaseTimer;

        // Timestamp of when the aggregate starts
        private DateTime _tsStart;

        // buffers for user input, they are emptied every x seconds (see Settings.UserInputAggregationInterval)
        private static readonly ConcurrentQueue<KeystrokeEvent> KeystrokeBuffer = new ConcurrentQueue<KeystrokeEvent>();
        private static readonly ConcurrentQueue<MouseClickEvent> MouseClickBuffer = new ConcurrentQueue<MouseClickEvent>();
        private static readonly ConcurrentQueue<MouseMovementSnapshot> MouseMoveBuffer = new ConcurrentQueue<MouseMovementSnapshot>();
        private static readonly ConcurrentQueue<MouseScrollSnapshot> MouseScrollBuffer = new ConcurrentQueue<MouseScrollSnapshot>();

        // ---- TODO. update comment
        private static readonly List<KeystrokeEvent> KeystrokeListToSave = new List<KeystrokeEvent>();
        private static readonly List<MouseClickEvent> MouseClickListToSave = new List<MouseClickEvent>();
        private static readonly List<MouseMovementSnapshot> MouseMoveListToSave = new List<MouseMovementSnapshot>();
        private static readonly List<MouseScrollSnapshot> MouseScrollListToSave = new List<MouseScrollSnapshot>();

        // temporary buffers to count up moves and scrolls, they are emptied every second after adding up (Settings.MouseSnapshotInterval)
        //private static readonly ConcurrentQueue<MouseMovementSnapshot> TempMouseMoveBuffer = new ConcurrentQueue<MouseMovementSnapshot>();
        //private static readonly ConcurrentQueue<MouseScrollSnapshot> TempMouseScrollBuffer = new ConcurrentQueue<MouseScrollSnapshot>();

        #endregion

        #region METHODS

        #region ITracker Stuff

        public Daemon()
        {
            Name = "User Input Tracker";
        }

        protected override  void Dispose(bool disposing)
        {
            if (! _disposed)
            {
                if (disposing)
                {
                    _saveToDatabaseTimer.Dispose();
                    //_mouseSnapshotTimer.Dispose();
                    _mEvents.Dispose();
                }

                // Release unmanaged resources.
                // Set large fields to null.                
                _disposed = true;
            }

            // Call Dispose on your base class.
            base.Dispose(disposing);
        }

        public override void Start()
        {
            // Register Save-To-Database Timer
            if (_saveToDatabaseTimer != null)
                Stop();
            _saveToDatabaseTimer = new Timer();
            _saveToDatabaseTimer.Interval = Settings.UserInputAggregationInterval.TotalMilliseconds;
            _saveToDatabaseTimer.Elapsed += SaveToDatabaseTick;
            _saveToDatabaseTimer.Start();

            // Register Mouse Movement/Scroll Timer
            //if (_mouseSnapshotTimer != null)
            //    Stop();
            //_mouseSnapshotTimer = new Timer();
            //_mouseSnapshotTimer.Interval = Settings.MouseSnapshotInterval.TotalMilliseconds;
            //_mouseSnapshotTimer.Elapsed += MouseSnapshotTick;
            //_mouseSnapshotTimer.Start();

            // Register Hooks for Mouse & Keyboard
            _mEvents = Hook.GlobalEvents();
            _mEvents.MouseWheel += MouseListener_MouseScrolling;
            _mEvents.MouseClick += MouseListener_MouseClick;
            _mEvents.MouseMoveExt += MouseListener_MouseMoveExt;
            _mEvents.KeyDown += KeyboardListener_KeyDown;

            // Set start timestamp for tracking
            _tsStart = DateTime.Now;

            IsRunning = true;
        }

        public override void Stop()
        {
            if (_saveToDatabaseTimer != null)
            {
                _saveToDatabaseTimer.Stop();
                _saveToDatabaseTimer.Dispose();
                _saveToDatabaseTimer = null;
            }

            //if (_mouseSnapshotTimer != null)
            //{
            //    _mouseSnapshotTimer.Stop();
            //    _mouseSnapshotTimer.Dispose();
            //    _mouseSnapshotTimer = null;
            //}

            // unregister mouse & keyboard events
            if (_mEvents != null)
            {
                _mEvents.MouseWheel -= MouseListener_MouseScrolling;
                _mEvents.MouseClick -= MouseListener_MouseClick;
                _mEvents.MouseMoveExt -= MouseListener_MouseMoveExt;
                _mEvents.KeyDown -= KeyboardListener_KeyDown;

                _mEvents.Dispose();
                _mEvents = null;
            }

            IsRunning = false;
        }

        public override List<IVisualization> GetVisualizationsDay(DateTimeOffset date)
        {
            var vis = new DayUserInputLineChart(date);
            return new List<IVisualization> { vis };
        }

        public override void CreateDatabaseTablesIfNotExist()
        {
            Queries.CreateUserInputTables();
        }

        public override void UpdateDatabaseTables(int version)
        {
            // no database updates necessary yet
        }

        public override bool IsEnabled()
        {
            return UserInputTrackerEnabled;
        }

        private bool _userInputTrackerEnabled;
        public bool UserInputTrackerEnabled
        {
            get
            {
                _userInputTrackerEnabled = Database.GetInstance().GetSettingsBool("UserInputTrackerEnabled", Settings.IsEnabledByDefault);
                return _userInputTrackerEnabled;
            }
            set
            {
                var updatedIsEnabled = value;

                // only update if settings changed
                if (updatedIsEnabled == _userInputTrackerEnabled) return;

                // update settings
                Database.GetInstance().SetSettings("UserInputTrackerEnabled", value);

                // start/stop tracker if necessary
                if (!updatedIsEnabled && IsRunning)
                {
                    Stop();
                }
                else if (updatedIsEnabled && !IsRunning)
                {
                    Start();
                }

                // log
                Database.GetInstance().LogInfo("The participant updated the setting 'UserInputTrackerEnabled' to " + updatedIsEnabled);
            }
        }

        #endregion

        #region Daemon Tracker: Events & manual Clicks (save to buffer)

        #region Prepare Buffers for saving in database (User Input Events)

        /// <summary>
        /// Mouse Click event. Create a new event and add it to the buffer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static async void MouseListener_MouseClick(object sender, MouseEventArgs e)
        {
            await Task.Run(() => MouseClickBuffer.Enqueue(new MouseClickEvent(e)));
        }

        /// <summary>
        /// Mouse scrolling event. Save it to a temp list to only save it ever x seconds to the database 
        /// (see Settings.MouseSnapshotInterval) to reduce the data load.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static async void MouseListener_MouseScrolling(object sender, MouseEventArgs e)
        {
            await Task.Run(() => MouseScrollBuffer.Enqueue(new MouseScrollSnapshot(e)));
        }

        /// <summary>
        /// Mouse Movement event. Save it to a temp list to only save it ever x seconds to the database 
        /// (see Settings.MouseSnapshotInterval) to reduce the data load.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static async void MouseListener_MouseMoveExt(object sender, MouseEventExtArgs e)
        {
            await Task.Run(() => MouseMoveBuffer.Enqueue(new MouseMovementSnapshot(e)));
        }

        /// <summary>
        /// Keyboard Click event. Create a new event and add it to the buffer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static async void KeyboardListener_KeyDown(object sender, KeyEventArgs e)
        {
            await Task.Run(() => KeystrokeBuffer.Enqueue(new KeystrokeEvent(e)));
        }

        #endregion

        #region Daemon Tracker: Persist (save to database)

        /// <summary>
        /// Saves the buffer to the database and clears it afterwards.
        /// </summary>
        private async void SaveToDatabaseTick(object sender, EventArgs e)
        {
            await Task.Run(() => SaveInputBufferToDatabase());
        }

        /// <summary>
        /// dequeues the currently counted number of elements from the buffer and saves them to the database
        /// (it can happen that more elements are added to the end of the queue while this happens,
        /// those elements will be safed to the database in the next run of this method)
        /// </summary>
        private void SaveInputBufferToDatabase()
        {
            try
            {
                var aggregate = new UserInputAggregate();

                // time interval to save
                var now = DateTime.Now;
                var tsEnd = now.AddSeconds(-now.Second).AddSeconds(-Settings.UserInputAggregationIntervalInSeconds);
                var tsStart = tsEnd.AddSeconds(-Settings.UserInputAggregationIntervalInSeconds);
                aggregate.TsStart = tsStart;
                aggregate.TsEnd = tsEnd;

                AddKeystrokesToAggregate(aggregate, tsStart, tsEnd);
                //AddMouseClicksToAggregate(aggregate, tsStart, tsEnd);
                //AddMouseScrollsToAggregate(aggregate, tsStart, tsEnd);
                //AddMouseMovementsToAggregate(aggregate, tsStart, tsEnd);

                // save aggregate to database
                Queries.SaveUserInputSnapshotToDatabase(aggregate);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        private void AddKeystrokesToAggregate(UserInputAggregate aggregate, DateTime tsStart, DateTime tsEnd)
        {
            // dequeue Keystroke Buffer
            KeystrokeEvent e;
            while (!KeystrokeBuffer.IsEmpty)
            {
                KeystrokeBuffer.TryDequeue(out e);
                KeystrokeListToSave.Add(e);
            }

            // save all items between tsStart - tsEnd
            if (KeystrokeListToSave == null || KeystrokeListToSave.Count == 0) return;
            
            var thisIntervalKeystrokes = KeystrokeListToSave.Where(i => i.Timestamp >= tsStart && i.Timestamp < tsEnd);
            aggregate.KeyNavigate = thisIntervalKeystrokes.Count(i => i.KeystrokeType == KeystrokeType.Navigate);
            aggregate.KeyBackspace = thisIntervalKeystrokes.Count(i => i.KeystrokeType == KeystrokeType.Backspace);
            aggregate.KeyOther = thisIntervalKeystrokes.Count(i => i.KeystrokeType == KeystrokeType.Key);
            aggregate.KeyTotal = aggregate.KeyNavigate + aggregate.KeyBackspace + aggregate.KeyOther;

            // delete all items older than tsEnd
            KeystrokeListToSave.RemoveAll(i => i.Timestamp < tsEnd);
        }

        private void AddMouseClicksToAggregate(UserInputAggregate aggregate, DateTime tsStart, DateTime tsEnd)
        {
            // TODO: do the same like keystroke (+ test keystroke again)

            if (MouseClickBuffer.Count <= 0) return;
            var buffer = MouseClickBuffer.Where(i => i.Timestamp >= tsStart && i.Timestamp < tsEnd);
            if (buffer == null || buffer.Count() == 0) return;

            aggregate.ClickLeft = buffer.Count(i => i.Button == MouseButtons.Left);
            aggregate.ClickRight = buffer.Count(i => i.Button == MouseButtons.Right);
            aggregate.ClickOther = buffer.Count(i => (i.Button != MouseButtons.Left && i.Button != MouseButtons.Right));
            aggregate.ClickTotal = aggregate.ClickLeft + aggregate.ClickRight + aggregate.ClickOther;

            // delete (everything until tsEnd)
            var bufferToDelete = MouseClickBuffer.Where(i => i.Timestamp < tsEnd);
            var deleting = MouseClickBuffer.Count - bufferToDelete.Count();
            MouseClickEvent deletedItem;
            for (int i = 0; i <= deleting; i++)
            {
                MouseClickBuffer.TryDequeue(out deletedItem);
            }
        }

        private void AddMouseScrollsToAggregate(UserInputAggregate aggregate, DateTime tsStart, DateTime tsEnd)
        {
            if (MouseScrollBuffer.Count <= 0) return;
            var buffer = MouseScrollBuffer.Where(i => i.Timestamp >= tsStart && i.Timestamp < tsEnd);
            if (buffer == null || buffer.Count() == 0) return;

            aggregate.ScrollDelta = buffer.Sum(i => Math.Abs(i.ScrollDelta));

            // delete (everything until tsEnd)
            var bufferToDelete = MouseScrollBuffer.Where(i => i.Timestamp < tsEnd);
            var deleting = MouseScrollBuffer.Count - bufferToDelete.Count();
            MouseScrollSnapshot deletedItem;
            for (int i = 0; i <= deleting; i++)
            {
                MouseScrollBuffer.TryDequeue(out deletedItem);
            }
        }

        private void AddMouseMovementsToAggregate(UserInputAggregate aggregate, DateTime tsStart, DateTime tsEnd)
        {
            if (MouseMoveBuffer.Count <= 0) return;
            var buffer = MouseMoveBuffer.Where(i => i.Timestamp >= tsStart && i.Timestamp < tsEnd);
            if (buffer == null || buffer.Count() == 0) return;

            aggregate.MovedDistance = (int)CalculateMouseMovementDistance(buffer);

            // delete (everything until tsEnd)
            var bufferToDelete = MouseMoveBuffer.Where(i => i.Timestamp < tsEnd);
            var deleting = MouseMoveBuffer.Count - bufferToDelete.Count();
            MouseMovementSnapshot deletedItem;
            for (int i = 0; i <= deleting; i++)
            {
                MouseMoveBuffer.TryDequeue(out deletedItem);
            }
        }

        /// <summary>
        /// Calculates the distance of the mouse movement in pixels.
        /// Could also be converted to centimeters or inches.
        /// </summary>
        /// <returns></returns>
        private static double CalculateMouseMovementDistance(IEnumerable<MouseMovementSnapshot> lastIntervalMouseMovements)
        {
            var distance = 0.0;
            if (lastIntervalMouseMovements == null) return distance;

            try
            {
                for (var i = 1; i < lastIntervalMouseMovements.Count(); i++)
                {
                    var previous = lastIntervalMouseMovements.ElementAt(i - 1);
                    var current = lastIntervalMouseMovements.ElementAt(i);

                    var x1 = previous.X;
                    var x2 = current.X;
                    var y1 = previous.Y;
                    var y2 = current.Y;

                    distance += Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
                }

                // here could be a conversion to centimeters or inches
                // see: http://stackoverflow.com/questions/13937093/calculate-distance-between-two-mouse-points
            }
            catch { }

            return distance;
        }

        #endregion

        #endregion

        #endregion
    }
}