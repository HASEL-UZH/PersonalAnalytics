// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// V1.0 Created: 2015-10-20
// V2.0 Created: 2016-11-28
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
using System.Reflection;

namespace UserInputTracker
{
    public sealed class Daemon : BaseTrackerDisposable, ITracker
    {
        #region FIELDS

        private bool _disposed = false;
        private IKeyboardMouseEvents _mEvents;
        private Timer _saveToDatabaseTimer;

        // Timestamp of when the aggregate starts
        private DateTime _tsStart;

        // Buffers for user input, they are emptied every x seconds (see Settings.UserInputAggregationInterval)
        private static readonly ConcurrentQueue<KeystrokeEvent> KeystrokeBuffer = new ConcurrentQueue<KeystrokeEvent>();
        private static readonly ConcurrentQueue<MouseClickEvent> MouseClickBuffer = new ConcurrentQueue<MouseClickEvent>();
        private static readonly ConcurrentQueue<MouseMovementSnapshot> MouseMovementBuffer = new ConcurrentQueue<MouseMovementSnapshot>();
        private static readonly ConcurrentQueue<MouseScrollSnapshot> MouseScrollsBuffer = new ConcurrentQueue<MouseScrollSnapshot>();

        // Lists which temporarily store all user input data until they are saved as an aggregate in the database
        private static readonly List<KeystrokeEvent> KeystrokeListToSave = new List<KeystrokeEvent>();
        private static readonly List<MouseClickEvent> MouseClickListToSave = new List<MouseClickEvent>();
        private static readonly List<MouseMovementSnapshot> MouseMovementListToSave = new List<MouseMovementSnapshot>();
        private static readonly List<MouseScrollSnapshot> MouseScrollsListToSave = new List<MouseScrollSnapshot>();

        #endregion

        #region METHODS

        #region ITracker Stuff

        public Daemon()
        {
            Name = "User Input Tracker";
            if (Settings.IsDetailedCollectionEnabled) Name += " (detailed)";
        }

        protected override  void Dispose(bool disposing)
        {
            if (! _disposed)
            {
                if (disposing)
                {
                    _saveToDatabaseTimer.Dispose();
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

        public override string GetVersion()
        {
            var v = new AssemblyName(Assembly.GetExecutingAssembly().FullName).Version;
            return Shared.Helpers.VersionHelper.GetFormattedVersion(v);
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
                    CreateDatabaseTablesIfNotExist();
                    Start();
                }

                // log
                Database.GetInstance().LogInfo("The participant updated the setting 'UserInputTrackerEnabled' to " + updatedIsEnabled);
            }
        }

        #endregion

        #region Provide Public Events

        public delegate void MouseClickEventHandler(MouseClickEvent m);
        public static event MouseClickEventHandler MouseClick;

        public delegate void MouseScrollingEventHandler(MouseScrollSnapshot m);
        public static event MouseScrollingEventHandler MouseScrolling;

        public delegate void MouseMovementEventHandler(MouseMovementSnapshot m);
        public static event MouseMovementEventHandler MouseMovement;

        public delegate void KeystrokeEventHandler(KeystrokeEvent m);
        public static event KeystrokeEventHandler Keystroke;

        #endregion

        #region Prepare Buffers for saving in database (User Input Events)

        /// <summary>
        /// Catch the mouse click event, save it in the buffer and forward the event in 
        /// case a client registered for it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static async void MouseListener_MouseClick(object sender, MouseEventArgs e)
        {
            await Task.Run(() =>
            {
                MouseClickBuffer.Enqueue(new MouseClickEvent(e));
                MouseClick?.Invoke(new MouseClickEvent(e));
            });
        }

        /// <summary>
        /// Catch the mouse scrolling event, save it in the buffer and forward the event in 
        /// case a client registered for it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static async void MouseListener_MouseScrolling(object sender, MouseEventArgs e)
        {
            await Task.Run(() =>
            {
                MouseScrollsBuffer.Enqueue(new MouseScrollSnapshot(e));
                MouseScrolling?.Invoke(new MouseScrollSnapshot(e));
            });
        }

        /// <summary>
        /// Catch the mouse movement event, save it in the buffer and forward the event in 
        /// case a client registered for it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static async void MouseListener_MouseMoveExt(object sender, MouseEventExtArgs e)
        {
            await Task.Run(() =>
            {
                MouseMovementBuffer.Enqueue(new MouseMovementSnapshot(e));
                MouseMovement?.Invoke(new MouseMovementSnapshot(e));
            });
        }

        /// <summary>
        /// Catch the keystroke event, save it in the buffer and forward the event in 
        /// case a client registered for it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static async void KeyboardListener_KeyDown(object sender, KeyEventArgs e)
        {
            await Task.Run(() =>
            {
                KeystrokeBuffer.Enqueue(new KeystrokeEvent(e));
                Keystroke?.Invoke(new KeystrokeEvent(e));
            });
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
                var tsEnd = now.AddSeconds(-now.Second).AddSeconds(-Settings.UserInputAggregationIntervalInSeconds); // round to minute, - 60s
                var tsStart = tsEnd.AddSeconds(-Settings.UserInputAggregationIntervalInSeconds); // tsEnd - 60s
                aggregate.TsStart = tsStart;
                aggregate.TsEnd = tsEnd;

                // sum up user input types in aggregate
                AddKeystrokesToAggregate(aggregate, tsStart, tsEnd);
                AddMouseClicksToAggregate(aggregate, tsStart, tsEnd);
                AddMouseScrollsToAggregate(aggregate, tsStart, tsEnd);
                AddMouseMovementsToAggregate(aggregate, tsStart, tsEnd);

                // save aggregate to database
                Queries.SaveUserInputSnapshotToDatabase(aggregate);
            }
            catch { }
        }

        /// <summary>
        /// For: Keystrokes Data
        /// Dequeues the respective buffer, adds it to a list (which prepares the data for saving)
        /// and updates the UserInputaggregate. Finally, it deletes the used items from the list.
        /// </summary>
        /// <param name="aggregate"></param>
        /// <param name="tsStart"></param>
        /// <param name="tsEnd"></param>
        private void AddKeystrokesToAggregate(UserInputAggregate aggregate, DateTime tsStart, DateTime tsEnd)
        {
            // dequeue buffer
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

            // if detailed user input logging for studies is enabled, save keystrokes separately
            if (Settings.IsDetailedCollectionEnabled) Queries.SaveKeystrokesToDatabase(thisIntervalKeystrokes.ToList());

            // delete all items older than tsEnd
            KeystrokeListToSave.RemoveAll(i => i.Timestamp < tsEnd);
        }

        /// <summary>
        /// For: Mouse Clicks Data
        /// Dequeues the respective buffer, adds it to a list (which prepares the data for saving)
        /// and updates the UserInputaggregate. Finally, it deletes the used items from the list.
        /// </summary>
        /// <param name="aggregate"></param>
        /// <param name="tsStart"></param>
        /// <param name="tsEnd"></param>
        private void AddMouseClicksToAggregate(UserInputAggregate aggregate, DateTime tsStart, DateTime tsEnd)
        {
            // dequeue buffer
            MouseClickEvent e;
            while (!MouseClickBuffer.IsEmpty)
            {
                MouseClickBuffer.TryDequeue(out e);
                MouseClickListToSave.Add(e);
            }

            // save all items between tsStart - tsEnd
            if (MouseClickListToSave == null || MouseClickListToSave.Count == 0) return;

            var thisIntervalMouseClicks = MouseClickListToSave.Where(i => i.Timestamp >= tsStart && i.Timestamp < tsEnd);
            aggregate.ClickLeft = thisIntervalMouseClicks.Count(i => i.Button == MouseButtons.Left);
            aggregate.ClickRight = thisIntervalMouseClicks.Count(i => i.Button == MouseButtons.Right);
            aggregate.ClickOther = thisIntervalMouseClicks.Count(i => (i.Button != MouseButtons.Left && i.Button != MouseButtons.Right));
            aggregate.ClickTotal = aggregate.ClickLeft + aggregate.ClickRight + aggregate.ClickOther;

            // if detailed user input logging for studies is enabled, save mouse clicks separately
            if (Settings.IsDetailedCollectionEnabled) Queries.SaveMouseClicksToDatabase(thisIntervalMouseClicks.ToList());

            // delete all items older than tsEnd
            MouseClickListToSave.RemoveAll(i => i.Timestamp < tsEnd);
        }

        /// <summary>
        /// For: Mouse Scrolls Data
        /// Dequeues the respective buffer, adds it to a list (which prepares the data for saving)
        /// and updates the UserInputaggregate. Finally, it deletes the used items from the list.
        /// </summary>
        /// <param name="aggregate"></param>
        /// <param name="tsStart"></param>
        /// <param name="tsEnd"></param>
        private void AddMouseScrollsToAggregate(UserInputAggregate aggregate, DateTime tsStart, DateTime tsEnd)
        {
            // dequeue buffer
            MouseScrollSnapshot e;
            while (!MouseScrollsBuffer.IsEmpty)
            {
                MouseScrollsBuffer.TryDequeue(out e);
                MouseScrollsListToSave.Add(e);
            }

            // save all items between tsStart - tsEnd
            if (MouseScrollsListToSave == null || MouseScrollsListToSave.Count == 0) return;

            var thisIntervalMouseScrolls = MouseScrollsListToSave.Where(i => i.Timestamp >= tsStart && i.Timestamp < tsEnd);
            aggregate.ScrollDelta = thisIntervalMouseScrolls.Sum(i => Math.Abs(i.ScrollDelta));

            // if detailed user input logging for studies is enabled, save mouse scrolls separately
            if (Settings.IsDetailedCollectionEnabled) SaveDetailedMouseScrolls(tsStart, tsEnd, thisIntervalMouseScrolls);

            // delete all items older than tsEnd
            MouseScrollsListToSave.RemoveAll(i => i.Timestamp < tsEnd);
        }

        /// <summary>
        /// Save the mouse scrolls per second
        /// (calculates the scrolled distance per second and saves it)
        /// </summary>
        /// <param name="tsStart"></param>
        /// <param name="tsEnd"></param>
        /// <param name="thisIntervalMouseMovements"></param>
        private void SaveDetailedMouseScrolls(DateTime tsStart, DateTime tsEnd, IEnumerable<MouseScrollSnapshot> thisIntervalMouseMovements)
        {
            var thisIntervalMouseScrollsPerSecond = new List<MouseScrollSnapshot>();

            var tsCurrent = tsStart;
            while (tsCurrent <= tsEnd)
            {
                // calculate moved pixels for this second
                var tsCurrentNext = tsCurrent.AddSeconds(1);
                var mouseScrollsForSecond = thisIntervalMouseMovements.Where(i => i.Timestamp >= tsCurrent && i.Timestamp < tsCurrentNext);

                if (mouseScrollsForSecond.Count() > 0)
                {
                    var scrolledPixels = mouseScrollsForSecond.Sum(i => Math.Abs(i.ScrollDelta));

                    // store if there are any
                    if (scrolledPixels > 0)
                    {
                        mouseScrollsForSecond.Last().ScrollDelta = (int)scrolledPixels;
                        thisIntervalMouseScrollsPerSecond.Add(mouseScrollsForSecond.Last());
                    }
                }

                tsCurrent = tsCurrentNext; // for next iteration
            }

            // save mouse moved per second to database
            Queries.SaveMouseScrollsToDatabase(thisIntervalMouseScrollsPerSecond);
        }

        /// <summary>
        /// For: Mouse Movement Data
        /// Dequeues the respective buffer, adds it to a list (which prepares the data for saving)
        /// and updates the UserInputaggregate. Finally, it deletes the used items from the list.
        /// </summary>
        /// <param name="aggregate"></param>
        /// <param name="tsStart"></param>
        /// <param name="tsEnd"></param>
        private void AddMouseMovementsToAggregate(UserInputAggregate aggregate, DateTime tsStart, DateTime tsEnd)
        {
            // dequeue buffer
            MouseMovementSnapshot e;
            while (!MouseMovementBuffer.IsEmpty)
            {
                MouseMovementBuffer.TryDequeue(out e);
                MouseMovementListToSave.Add(e);
            }

            // save all items between tsStart - tsEnd
            if (MouseMovementListToSave == null || MouseMovementListToSave.Count == 0) return;

            var thisIntervalMouseMovements = MouseMovementListToSave.Where(i => i.Timestamp >= tsStart && i.Timestamp < tsEnd);
            aggregate.MovedDistance = (int)CalculateMouseMovementDistance(thisIntervalMouseMovements);

            // if detailed user input logging for studies is enabled, save mouse movements separately
            if (Settings.IsDetailedCollectionEnabled) SaveDetailedMouseMovements(tsStart, tsEnd, thisIntervalMouseMovements);

            // delete all items older than tsEnd
            MouseMovementListToSave.RemoveAll(i => i.Timestamp < tsEnd);
        }

        /// <summary>
        /// Save the mouse movements per second
        /// (calculates the moved distance per second and saves it)
        /// </summary>
        /// <param name="tsStart"></param>
        /// <param name="tsEnd"></param>
        /// <param name="thisIntervalMouseMovements"></param>
        private void SaveDetailedMouseMovements(DateTime tsStart, DateTime tsEnd, IEnumerable<MouseMovementSnapshot> thisIntervalMouseMovements)
        {
            var thisIntervalMouseMovementsPerSecond = new List<MouseMovementSnapshot>();

            var tsCurrent = tsStart;
            while (tsCurrent <= tsEnd)
            {
                // calculate moved pixels for this second
                var tsCurrentNext = tsCurrent.AddSeconds(1);
                var mouseMovementsForSecond = thisIntervalMouseMovements.Where(i => i.Timestamp >= tsCurrent && i.Timestamp < tsCurrentNext);

                if (mouseMovementsForSecond.Count() > 0)
                {
                    var movedPixels = CalculateMouseMovementDistance(mouseMovementsForSecond);

                    // store if there are any
                    if (movedPixels > 0)
                    {
                        mouseMovementsForSecond.Last().MovedDistance = (int)movedPixels;
                        thisIntervalMouseMovementsPerSecond.Add(mouseMovementsForSecond.Last());
                    }
                }

                tsCurrent = tsCurrentNext; // for next iteration
            }

            // save mouse moved per second to database
            Queries.SaveMouseMovementsToDatabase(thisIntervalMouseMovementsPerSecond);
        }

        /// <summary>
        /// Calculates the distance of the mouse movement in pixels.
        /// Could also be converted to centimeters or inches.
        /// </summary>
        /// <returns></returns>
        public static double CalculateMouseMovementDistance(IEnumerable<MouseMovementSnapshot> lastIntervalMouseMovements)
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
    }
}