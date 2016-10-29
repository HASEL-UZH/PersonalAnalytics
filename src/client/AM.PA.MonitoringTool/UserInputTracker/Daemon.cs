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

        #region Prepare Buffers for saving in database

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
        /// Saves the mouse scrolls and mouse movements to the buffer after summing up.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private static async void MouseSnapshotTick(object sender, EventArgs e)
        //{
        //    //run new threads to dequeue temporary mouse buffers, and enqueue the calculated values into the more longterm buffers
        //    await Task.Run(() => AddMouseScrollsToInputBuffer());
        //    await Task.Run(() => AddMouseMovementToInputBuffer());
        //}

        /// <summary>
        /// Calculates the distance scrolled for the last interval and adds it to the inputbuffer (to be stored
        /// in the database) if there was some scrolling.
        /// </summary>
        //private static void AddMouseScrollsToInputBuffer() {
        //{
        //    if (TempMouseScrollBuffer == null || TempMouseScrollBuffer.Count == 0) return;

        //    try
        //    {
        //        //dequeue thread-safely from temp buffer
        //        int count = TempMouseScrollBuffer.Count;
        //        MouseScrollSnapshot[] lastIntervalMouseScrolls = new MouseScrollSnapshot[count];
        //        for (int i = 0; i < count; i++)
        //        {
        //            TempMouseScrollBuffer.TryDequeue(out lastIntervalMouseScrolls[i]);
        //        }

        //        //calculate scroll distance and enqueue to the long term buffer
        //        var scrollDistance = lastIntervalMouseScrolls.Sum(scroll => scroll.ScrollDelta);
        //        var lastSnapshot = lastIntervalMouseScrolls[count - 1]; //last element in queue is the newest
        //        lastSnapshot.ScrollDelta += Math.Abs(scrollDistance);
        //        MouseScrollBuffer.Enqueue(lastSnapshot);
        //    }
        //    catch { }
        //}

        /// <summary>
        /// Calculates the distance moved for the last interval and adds it to the inputbuffer (to be stored
        /// in the database) if there was some mouse movement.
        /// </summary>
        //private static void AddMouseMovementToInputBuffer()
        //{
        //    if (TempMouseMoveBuffer == null || TempMouseMoveBuffer.Count == 0) return;

        //    try
        //    {
        //        //dequeue thread-safely from temp buffer
        //        var count = TempMouseMoveBuffer.Count;
        //        var lastIntervalMouseMovements = new MouseMovementSnapshot[count];
        //        for (int i = 0; i < count; i++)
        //        {
        //            TempMouseMoveBuffer.TryDequeue(out lastIntervalMouseMovements[i]);
        //        }

        //        //calculate scroll distance and enqueue to the long term buffer
        //        var lastSnapshot = lastIntervalMouseMovements[count - 1];
        //        var movementDistance = CalculateMouseMovementDistance(lastIntervalMouseMovements);
        //        lastSnapshot.MovedDistance = (int)movementDistance;
        //        MouseMoveBuffer.Enqueue(lastSnapshot);
        //    }
        //    catch { }
        //}

        /// <summary>
        /// Calculates the distance of the mouse movement in pixels.
        /// Could also be converted to centimeters or inches.
        /// </summary>
        /// <returns></returns>
        private static double CalculateMouseMovementDistance(IReadOnlyList<MouseMovementSnapshot> lastIntervalMouseMovements)
        {
            var distance = 0.0;
            if (lastIntervalMouseMovements == null) return distance;

            try
            {
                for (var i = 1; i < lastIntervalMouseMovements.Count; i++)
                {
                    var previous = lastIntervalMouseMovements[i - 1];
                    var current = lastIntervalMouseMovements[i];

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
            // throw and save
            await Task.Run(() => SaveInputBufferToDatabase());
        }

        /// <summary>
        /// dequeues the currently counted number of elements from the buffer and safes them to the database
        /// (it can happen that more elements are added to the end of the queue while this happens,
        /// those elements will be safed to the database in the next run of this method)
        /// </summary>
        private void SaveInputBufferToDatabase()
        {
            try
            {
                var aggregate = new UserInputAggregate();

                // Keystrokes
                if (KeystrokeBuffer.Count > 0)
                {
                    // dequeue buffer
                    var keystrokes = new KeystrokeEvent[KeystrokeBuffer.Count];
                    for (var i = 0; i < KeystrokeBuffer.Count; i++)
                    {
                        KeystrokeBuffer.TryDequeue(out keystrokes[i]);
                    }

                    // sum up
                    for (var j = 0; j < keystrokes.Count(); j++)
                    {
                        var item = (KeystrokeEvent)keystrokes[j];
                        if (item == null) continue;

                        aggregate.KeyTotal += 1;

                        switch (item.KeystrokeType)
                        {
                            case KeystrokeType.Backspace:
                                aggregate.KeyBackspace += 1;
                                break;
                            case KeystrokeType.Navigate:
                                aggregate.KeyNavigate += 1;
                                break;
                            case KeystrokeType.Key:
                                aggregate.KeyOther += 1;
                                break;
                        }
                    }

                    keystrokes = null;
                }

                // Mouse Clicks
                if (MouseClickBuffer.Count > 0)
                {
                    // dequeue buffer
                    var mouseClicks = new MouseClickEvent[MouseClickBuffer.Count];
                    for (int i = 0; i < MouseClickBuffer.Count; i++)
                    {
                        MouseClickBuffer.TryDequeue(out mouseClicks[i]);
                    }

                    // sum up 
                    for (var j = 0; j < mouseClicks.Count(); j++)
                    {
                        var item = (MouseClickEvent)mouseClicks[j];
                        if (item == null) continue;

                        aggregate.ClickTotal += 1;

                        switch (item.Button)
                        {
                            case MouseButtons.Left:
                                aggregate.ClickLeft += 1;
                                break;
                            case MouseButtons.Right:
                                aggregate.ClickRight += 1;
                                break;
                            default:
                                aggregate.ClickOther += 1;
                                break;
                        }
                    }

                    mouseClicks = null;
                }

                // Mouse Scrolls
                if (MouseScrollBuffer.Count > 0)
                {
                    // dequeue buffer
                    var mouseScrolls = new MouseScrollSnapshot[MouseScrollBuffer.Count];
                    for (int i = 0; i < MouseScrollBuffer.Count; i++)
                    {
                        MouseScrollBuffer.TryDequeue(out mouseScrolls[i]);
                    }

                    // sum up
                    for (var j = 0; j < mouseScrolls.Count(); j++)
                    {
                        var item = (MouseScrollSnapshot)mouseScrolls[j];
                        if (item == null) continue;
                        aggregate.ScrollDelta += Math.Abs(item.ScrollDelta);
                    }

                    mouseScrolls = null;
                }

                // Mouse Movements
                if (MouseMoveBuffer.Count > 0)
                {
                    // dequeue buffer
                    var mouseMovements = new MouseMovementSnapshot[MouseMoveBuffer.Count];
                    for (int i = 0; i < MouseMoveBuffer.Count; i++)
                    {
                        MouseMoveBuffer.TryDequeue(out mouseMovements[i]);
                    }

                    // sum up
                    aggregate.MovedDistance = (int)CalculateMouseMovementDistance(mouseMovements);
                    //for (var j = 0; j < mouseMovements.Count(); j++)
                    //{
                    //    var item = (MouseMovementSnapshot)mouseMovements[j];
                    //    if (item == null) continue;
                    //    aggregate.MovedDistance += Math.Abs(item.MovedDistance);
                    //}

                    mouseMovements = null;
                }

                // todo: add timestamps
                var _tsEnd = DateTime.Now;
                aggregate.TsStart = _tsStart;
                aggregate.TsEnd = _tsEnd;
                _tsStart = _tsEnd;

                // save aggregate to database
                Queries.SaveUserInputSnapshotToDatabase(aggregate);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        #endregion

        //#region Calculate User Input

        //public static int GetNumberOfKeystrokes(DateTime startTime, DateTime endTime)
        //{
        //    var returning = KeystrokeBuffer.Where(x => x.Timestamp > startTime && x.Timestamp < endTime).Count();
        //    var saving = KeystrokeBuffer.Count - returning;

        //    KeystrokeEvent e;
        //    for (int i = 0; i <= saving; i++)
        //    {
        //        if (KeystrokeBuffer.TryDequeue(out e))
        //        {
        //            SaveKeystrokeBuffer.Push(e);
        //        }
        //    }

        //    return returning;
        //}

        //public static int GetNumberOfMouseClicks(DateTime startTime, DateTime endTime)
        //{
        //    var returning = MouseClickBuffer.Where(x => x.Timestamp > startTime && x.Timestamp < endTime).Count();
        //    var saving = MouseClickBuffer.Count - returning;

        //    MouseClickEvent e;
        //    for (int i = 0; i <= saving; i++)
        //    {
        //        if (MouseClickBuffer.TryDequeue(out e))
        //        {
        //            SaveMouseClickBuffer.Push(e);
        //        }
        //    }

        //    return returning;
        //}

        //public static double GetTotalLogMouseScrollDelta(DateTime startTime, DateTime endTime)
        //{
        //    var buffer = MouseScrollBuffer.Where(x => x.Timestamp > startTime && x.Timestamp < endTime);
        //    double sumOfLogScrollDelta = 0;
        //    foreach (var snapshot in buffer)
        //    {
        //        long scrollDelta = (long)snapshot.ScrollDelta;
        //        sumOfLogScrollDelta += Math.Log(Math.Abs(scrollDelta) + 1);
        //    }

        //    var saving = MouseScrollBuffer.Count - buffer.Count();

        //    MouseScrollSnapshot e;
        //    for (int i = 0; i <= saving; i++)
        //    {
        //        if (MouseScrollBuffer.TryDequeue(out e))
        //        {
        //            SaveMouseScrollBuffer.Push(e);
        //        }
        //    }

        //    return sumOfLogScrollDelta;
        //}

        //public static int GetTotalMouseMovedDistance(DateTime startTime, DateTime endTime)
        //{
        //    var buffer = MouseMoveBuffer.Where(x => x.Timestamp > startTime && x.Timestamp < endTime);
        //    int distance = buffer.Sum(x => x.MovedDistance);

        //    var saving = MouseMoveBuffer.Count - buffer.Count();

        //    MouseMovementSnapshot e;
        //    for (int i = 0; i <= saving; i++)
        //    {
        //        if (MouseMoveBuffer.TryDequeue(out e))
        //        {
        //            SaveMouseMoveBuffer.Push(e);
        //        }
        //    }

        //    return distance;
        //}

        //#endregion

        #endregion

        #endregion
    }
}