using System;
using System.Timers;
using WindowRecommender.Native;

namespace WindowRecommender
{
    internal class ModelEvents
    {
        internal event EventHandler<IntPtr> WindowFocused;
        internal event EventHandler<IntPtr> WindowOpened;
        internal event EventHandler<IntPtr> WindowClosed;
        internal event EventHandler MoveStarted;
        internal event EventHandler MoveEnded;

        private readonly NativeMethods.Wineventproc _onWindowCreated;
        private readonly NativeMethods.Wineventproc _onWindowFocused;
        private readonly NativeMethods.Wineventproc _onWindowRestore;
        private readonly NativeMethods.Wineventproc _onWindowMinimize;
        private readonly NativeMethods.Wineventproc _onWindowClosed;
        private readonly NativeMethods.Wineventproc _onWindowMoved;
        private readonly NativeMethods.Wineventproc _onMoveStarted;
        private readonly NativeMethods.Wineventproc _onMoveEnded;

        private IntPtr[] _winEventHooks;
        private bool _isMoving;
        private IntPtr _focusedWindow;

        internal ModelEvents()
        {
            _onWindowCreated = OnWindowCreated;
            _onWindowFocused = OnWindowFocused;
            _onWindowRestore = OnWindowFocused;
            _onWindowClosed = OnWindowClosed;
            _onWindowMinimize = OnWindowClosed;
            _onWindowMoved = OnWindowMoved;
            _onMoveStarted = OnMoveStarted;
            _onMoveEnded = OnMoveEnded;
        }

        public void Start()
        {
            _isMoving = false;

            _winEventHooks = new[]
            {
                NativeMethods.SetWinEventHook(WinEventConstant.EVENT_OBJECT_CREATE, _onWindowCreated),
                NativeMethods.SetWinEventHook(WinEventConstant.EVENT_SYSTEM_FOREGROUND, _onWindowFocused),
                NativeMethods.SetWinEventHook(WinEventConstant.EVENT_SYSTEM_MINIMIZEEND, _onWindowRestore),
                NativeMethods.SetWinEventHook(WinEventConstant.EVENT_SYSTEM_MINIMIZESTART, _onWindowMinimize),
                NativeMethods.SetWinEventHook(WinEventConstant.EVENT_SYSTEM_MOVESIZEEND, _onMoveEnded),
                NativeMethods.SetWinEventHook(WinEventConstant.EVENT_SYSTEM_MOVESIZESTART, _onMoveStarted),
                NativeMethods.SetWinEventHook(WinEventConstant.EVENT_OBJECT_LOCATIONCHANGE, _onWindowMoved),
                NativeMethods.SetWinEventHook(WinEventConstant.EVENT_OBJECT_DESTROY, _onWindowClosed)
            };
        }

        private void OnWindowCreated(IntPtr hWinEventHook, WinEventConstant @event, IntPtr hwnd, ObjectIdentifier idObject, int idChild, uint idEventThread, uint dwmsEventTime)
        {
            if (hwnd != IntPtr.Zero && idObject == ObjectIdentifier.OBJID_WINDOW && idChild == NativeMethods.CHILDID_SELF)
            {
                if (NativeMethods.IsOpenWindow(hwnd))
                {
                    WindowOpened?.Invoke(this, hwnd);
                }
                else
                {
                    // Opening windows can take a bit to be recognized as visible.
                    // As there is no further event, wait for 1s to see if it becomes visible.
                    // Use the Timer's AutoReset once to have two checks at 500ms each.
                    var timer = new Timer(500);
                    timer.Elapsed += (sender, args) =>
                    {
                        var elapsedTimer = (Timer)sender;
                        elapsedTimer.AutoReset = false;
                        if (NativeMethods.IsOpenWindow(hwnd))
                        {
                            elapsedTimer.Stop();
                            elapsedTimer.Dispose();
                            WindowOpened?.Invoke(this, hwnd);
                        }
                    };
                    timer.Start();
                }
            }
        }

        private void OnWindowFocused(IntPtr hWinEventHook, WinEventConstant @event, IntPtr hwnd, ObjectIdentifier idObject, int idChild, uint idEventThread, uint dwmsEventTime)
        {
            if (hwnd != IntPtr.Zero && idObject == ObjectIdentifier.OBJID_WINDOW && idChild == NativeMethods.CHILDID_SELF && NativeMethods.IsOpenWindow(hwnd))
            {
                if (!_isMoving && hwnd != _focusedWindow)
                {
                    WindowFocused?.Invoke(this, hwnd);
                    _focusedWindow = hwnd;
                }
            }
        }

        private void OnWindowMoved(IntPtr hWinEventHook, WinEventConstant @event, IntPtr hwnd, ObjectIdentifier idObject, int idChild, uint idEventThread, uint dwmsEventTime)
        {
            if (hwnd != IntPtr.Zero && idObject == ObjectIdentifier.OBJID_WINDOW && idChild == NativeMethods.CHILDID_SELF && NativeMethods.IsOpenWindow(hwnd))
            {
                if (!_isMoving && hwnd == _focusedWindow)
                {
                    MoveEnded?.Invoke(this, null);
                }
            }
        }

        private void OnWindowClosed(IntPtr hWinEventHook, WinEventConstant @event, IntPtr hwnd, ObjectIdentifier idObject, int idChild, uint idEventThread, uint dwmsEventTime)
        {
            if (hwnd != IntPtr.Zero && idObject == ObjectIdentifier.OBJID_WINDOW && idChild == NativeMethods.CHILDID_SELF)
            {
                if (!_isMoving)
                {
                    WindowClosed?.Invoke(this, hwnd);
                    if (hwnd == _focusedWindow)
                    {
                        _focusedWindow = IntPtr.Zero;
                    }
                }
            }
        }

        private void OnMoveStarted(IntPtr hWinEventHook, WinEventConstant @event, IntPtr hwnd, ObjectIdentifier idObject, int idChild, uint idEventThread, uint dwmsEventTime)
        {
            if (hwnd != IntPtr.Zero && idObject == ObjectIdentifier.OBJID_WINDOW && idChild == NativeMethods.CHILDID_SELF)
            {
                _isMoving = true;
                MoveStarted?.Invoke(this, null);
            }
        }

        private void OnMoveEnded(IntPtr hWinEventHook, WinEventConstant @event, IntPtr hwnd, ObjectIdentifier idObject, int idChild, uint idEventThread, uint dwmsEventTime)
        {
            if (hwnd != IntPtr.Zero && idObject == ObjectIdentifier.OBJID_WINDOW && idChild == NativeMethods.CHILDID_SELF)
            {
                _isMoving = false;
                MoveEnded?.Invoke(this, null);
            }
        }

        public void Stop()
        {
            foreach (var winEventHook in _winEventHooks)
            {
                NativeMethods.UnhookWinEvent(winEventHook);
            }
            _winEventHooks = new IntPtr[0];
            _focusedWindow = IntPtr.Zero;
        }
    }
}
