using System;
using EyeCatcher.Native;
using EyeCatcherLib;

namespace EyeCatcher.DataCollection
{
    public class ProcessHook : IDisposable
    {
        private readonly NativeMethods.WinEventDelegate _moveSizeEndCallback;
        private readonly NativeMethods.WinEventDelegate _minimizeCallback;
        private readonly NativeMethods.WinEventDelegate _minimizeEndCallback;
        private readonly NativeMethods.WinEventDelegate _renameCallback;
        private readonly NativeMethods.WinEventDelegate _destroyCallback;

        public ProcessHook(uint processId, IWindowRecordCollection windows)
        {
            ProcessId = processId;
            _moveSizeEndCallback = (hook, type, hwnd, idObject, idChild, thread, time) => windows.MoveOrResize(hwnd);
            _minimizeCallback = (hook, type, hwnd, idObject, idChild, thread, time) => windows.Minimize(hwnd);
            _minimizeEndCallback = (hook, type, hwnd, idObject, idChild, thread, time) => windows.MinimizeEnd(hwnd);
            _renameCallback = (hook, type, hwnd, idObject, idChild, thread, time) =>
            {
                if (idObject == ObjectIdentifier.OBJID_WINDOW && idChild == NativeMethods.CHILDID_SELF)
                {
                    windows.Rename(hwnd);
                }
            };
            _destroyCallback = (hook, type, hwnd, idObject, idChild, thread, time) =>
            {
                if (idObject == ObjectIdentifier.OBJID_WINDOW && idChild == NativeMethods.CHILDID_SELF)
                {
                    windows.Destroy(hwnd);
                }
            };

            // TODO RR: EVENT_OBJECT_SHOW
            // TODO RR: EVENT_OBJECT_HIDE
            // TODO RR: EVENT_OBJECT_REORDER (This event is also sent by a parent window when the Z-order for the child windows changes.)

            // GetAltTabInfo https://msdn.microsoft.com/en-us/library/windows/desktop/ms633501(v=vs.85).aspx
        }

        public uint ProcessId { get; }

        private IntPtr WindowMoveSizeEndHook { get; set; }
        private IntPtr WindowMinimizeHook { get; set; }
        private IntPtr WindowMinimizeEndHook { get; set; }
        private IntPtr ObjectNameChangeHook { get; set; }
        private IntPtr ObjectDestroyHook { get; set; }

        public void ActivateHooks()
        {
            WindowMoveSizeEndHook = NativeMethods.SetWinEventHook(EventConstant.EVENT_SYSTEM_MOVESIZEEND, _moveSizeEndCallback, ProcessId);
            WindowMinimizeHook = NativeMethods.SetWinEventHook(EventConstant.EVENT_SYSTEM_MINIMIZESTART, _minimizeCallback, ProcessId);
            WindowMinimizeEndHook = NativeMethods.SetWinEventHook(EventConstant.EVENT_SYSTEM_MINIMIZEEND, _minimizeEndCallback, ProcessId);
            
            ObjectNameChangeHook = NativeMethods.SetWinEventHook(EventConstant.EVENT_OBJECT_NAMECHANGE, _renameCallback, ProcessId);
            ObjectDestroyHook = NativeMethods.SetWinEventHook(EventConstant.EVENT_OBJECT_DESTROY, _destroyCallback, ProcessId);
        }

        private void ReleaseUnmanagedResources()
        {
            UnhookWinEvent(WindowMoveSizeEndHook);
            UnhookWinEvent(WindowMinimizeHook);
            UnhookWinEvent(WindowMinimizeEndHook);
            UnhookWinEvent(ObjectNameChangeHook);
            UnhookWinEvent(ObjectDestroyHook);
        }

        private static void UnhookWinEvent(IntPtr hookPtr)
        {
            if (hookPtr != IntPtr.Zero)
            {
                NativeMethods.UnhookWinEvent(hookPtr);
            }
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~ProcessHook()
        {
            ReleaseUnmanagedResources();
        }
    }
}
