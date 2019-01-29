using System;
using WindowRecommender.Native;

namespace WindowRecommender
{
    internal class ModelEvents
    {
        internal event EventHandler<string> WindowAdded;
        internal event EventHandler<string> WindowFocused;
        internal event EventHandler<string> WindowClosed;
        internal event EventHandler AllWindowsBlurred;

        private NativeMethods.Wineventproc _onWindowFocused;
        private IntPtr _eventPtr;

        public void Start()
        {
            _onWindowFocused = OnWindowFocused;
            _eventPtr = NativeMethods.SetWinEventHook(NativeMethods.EVENT_SYSTEM_FOREGROUND, _onWindowFocused);
            _eventPtr = NativeMethods.SetWinEventHook(NativeMethods.EVENT_SYSTEM_MINIMIZEEND, _onWindowFocused);
        }

        private void OnWindowFocused(IntPtr hWinEventHook, uint @event, IntPtr hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime)
        {
            if (hwnd != IntPtr.Zero && idObject == NativeMethods.OBJID_WINDOW && idChild == NativeMethods.CHILDID_SELF)
            {
                WindowFocused?.Invoke(this, hwnd.ToString());
            }
        }

        public void Stop()
        {
            NativeMethods.UnhookWinEvent(_eventPtr);
        }
    }
}
