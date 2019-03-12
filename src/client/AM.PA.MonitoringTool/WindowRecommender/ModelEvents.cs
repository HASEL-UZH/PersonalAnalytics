using System;
using System.Collections.Generic;
using WindowRecommender.Native;

namespace WindowRecommender
{
    internal class ModelEvents
    {
        internal event EventHandler<IntPtr> WindowFocused;
        internal event EventHandler<IntPtr> WindowClosed;
        internal event EventHandler MoveStarted;
        internal event EventHandler MoveEnded;

        private readonly NativeMethods.Wineventproc _onWindowFocused;
        private readonly NativeMethods.Wineventproc _onWindowClosed;
        private readonly NativeMethods.Wineventproc _onMoveStarted;
        private readonly NativeMethods.Wineventproc _onMoveEnded;

        private List<IntPtr> _winEventHooks;
        private bool _isMoving;
        private IntPtr _focusedWindow;

        internal ModelEvents()
        {
            _onWindowFocused = OnWindowFocused;
            _onWindowClosed = OnWindowClosed;
            _onMoveStarted = OnMoveStarted;
            _onMoveEnded = OnMoveEnded;
        }

        public void Start()
        {
            _isMoving = false;

            _winEventHooks = new List<IntPtr>
            {
                NativeMethods.SetWinEventHook(WinEventConstant.EVENT_SYSTEM_FOREGROUND, _onWindowFocused),
                NativeMethods.SetWinEventHook(WinEventConstant.EVENT_SYSTEM_MINIMIZEEND, _onWindowFocused),
                NativeMethods.SetWinEventHook(WinEventConstant.EVENT_SYSTEM_MOVESIZEEND, _onMoveEnded),
                NativeMethods.SetWinEventHook(WinEventConstant.EVENT_SYSTEM_MOVESIZESTART, _onMoveStarted),
                NativeMethods.SetWinEventHook(WinEventConstant.EVENT_OBJECT_LOCATIONCHANGE, _onWindowFocused),
                NativeMethods.SetWinEventHook(WinEventConstant.EVENT_OBJECT_DESTROY, _onWindowClosed),
            };
        }

        private void OnWindowFocused(IntPtr hWinEventHook, WinEventConstant @event, IntPtr hwnd, ObjectIdentifier idObject, int idChild, uint idEventThread, uint dwmsEventTime)
        {
            if (hwnd != IntPtr.Zero && idObject == ObjectIdentifier.OBJID_WINDOW && idChild == NativeMethods.CHILDID_SELF && NativeMethods.IsOpenWindow(hwnd))
            {
                if (!_isMoving)
                {
                    if (hwnd != _focusedWindow)
                    {
                        WindowFocused?.Invoke(this, hwnd);
                        _focusedWindow = hwnd;
                    }
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
            _winEventHooks.Clear();
            _focusedWindow = IntPtr.Zero;
        }
    }
}
