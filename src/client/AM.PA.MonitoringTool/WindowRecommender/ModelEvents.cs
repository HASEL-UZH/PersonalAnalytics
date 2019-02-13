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
        private List<IntPtr> _winEventHooks;

        public void Start()
        {
            _onWindowFocused = OnWindowFocused;
            _onMoveStarted = OnMoveStarted;

            _winEventHooks = new List<IntPtr>
            {
                NativeMethods.SetWinEventHook(WinEventConstant.EVENT_SYSTEM_FOREGROUND, _onWindowFocused),
                NativeMethods.SetWinEventHook(WinEventConstant.EVENT_SYSTEM_MINIMIZEEND, _onWindowFocused),
                NativeMethods.SetWinEventHook(WinEventConstant.EVENT_SYSTEM_MOVESIZEEND, _onWindowFocused),
                NativeMethods.SetWinEventHook(WinEventConstant.EVENT_SYSTEM_MOVESIZESTART, _onMoveStarted),
            };
        }

        private void OnWindowFocused(IntPtr hWinEventHook, WinEventConstant @event, IntPtr hwnd, ObjectIdentifier idObject, int idChild, uint idEventThread, uint dwmsEventTime)
        {
            if (hwnd != IntPtr.Zero && idObject == ObjectIdentifier.OBJID_WINDOW && idChild == NativeMethods.CHILDID_SELF)
            {
                WindowFocused?.Invoke(this, hwnd.ToString());
            }
        }

        private void OnMoveStarted(IntPtr hWinEventHook, WinEventConstant @event, IntPtr hwnd, ObjectIdentifier idObject, int idChild, uint idEventThread, uint dwmsEventTime)
        {
            if (hwnd != IntPtr.Zero && idObject == ObjectIdentifier.OBJID_WINDOW && idChild == NativeMethods.CHILDID_SELF)
            {
                MoveStarted?.Invoke(this, null);
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
