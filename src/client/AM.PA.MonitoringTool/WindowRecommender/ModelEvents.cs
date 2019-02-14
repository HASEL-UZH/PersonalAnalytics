using System;
using System.Collections.Generic;
using WindowRecommender.Native;

namespace WindowRecommender
{
    internal class ModelEvents
    {
        internal event EventHandler<string> WindowAdded;
        internal event EventHandler<string> WindowFocused;
        internal event EventHandler<string> WindowClosed;
        internal event EventHandler AllWindowsBlurred;
        internal event EventHandler MoveStarted;

        private NativeMethods.Wineventproc _onWindowFocused;
        private NativeMethods.Wineventproc _onMoveStarted;
        private NativeMethods.Wineventproc _onMoveEnded;
        private List<IntPtr> _winEventHooks;
        private bool _isMoving;

        public void Start()
        {
            _onWindowFocused = OnWindowFocused;
            _onMoveStarted = OnMoveStarted;
            _onMoveEnded = OnMoveEnded;

            _isMoving = false;

            _winEventHooks = new List<IntPtr>
            {
                NativeMethods.SetWinEventHook(WinEventConstant.EVENT_SYSTEM_FOREGROUND, _onWindowFocused),
                NativeMethods.SetWinEventHook(WinEventConstant.EVENT_SYSTEM_MINIMIZEEND, _onWindowFocused),
                NativeMethods.SetWinEventHook(WinEventConstant.EVENT_SYSTEM_MOVESIZEEND, _onMoveEnded),
                NativeMethods.SetWinEventHook(WinEventConstant.EVENT_SYSTEM_MOVESIZESTART, _onMoveStarted),
                NativeMethods.SetWinEventHook(WinEventConstant.EVENT_OBJECT_LOCATIONCHANGE, _onWindowFocused),
            };
        }

        private void OnWindowFocused(IntPtr hWinEventHook, WinEventConstant @event, IntPtr hwnd, ObjectIdentifier idObject, int idChild, uint idEventThread, uint dwmsEventTime)
        {
            if (hwnd != IntPtr.Zero && idObject == ObjectIdentifier.OBJID_WINDOW && idChild == NativeMethods.CHILDID_SELF)
            {
                if (!_isMoving)
                {
                    WindowFocused?.Invoke(this, hwnd.ToString());
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
                WindowFocused?.Invoke(this, null);
            }
        }

        public void Stop()
        {
            foreach (var winEventHook in _winEventHooks)
            {
                NativeMethods.UnhookWinEvent(winEventHook);
            }
        }
    }
}
