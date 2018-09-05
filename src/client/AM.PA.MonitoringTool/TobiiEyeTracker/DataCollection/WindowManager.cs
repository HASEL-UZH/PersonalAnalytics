using System;
using System.Collections.Concurrent;
using EyeCatcher.Native;
using EyeCatcherDatabase.Records;
using EyeCatcherLib;

namespace EyeCatcher.DataCollection
{

    /// <inheritdoc />
    /// <summary>
    /// Window Lifecycle Management
    /// Creating Callbacks for the following events
    /// 1. Create (always calls ->Activate)
    /// 2. Move, Resize, Deactivate, Minimize, Maximize, Rename
    /// 3. Destroy
    /// ONLY WORKS WITH A RUNNING MESSAGELOOP (WPF/Winforms/Custom)
    /// MUST BE RUN FROM DISPATCHER THREAD
    /// https://docs.microsoft.com/en-us/windows/desktop/winmsg/using-windows
    /// </summary>
    public class WindowManager : IDisposable
    {
        private readonly NativeMethods.WinEventDelegate _activateCallback;
        private IntPtr _activateWindowHook;

        // ReSharper disable once UnusedMember.Global Justification: IOC
        public WindowManager(ObservableWindowRecordCollection windowRecordCollection)
        {
            Windows = windowRecordCollection ?? throw new ArgumentNullException(nameof(windowRecordCollection));
            _activateCallback = (hook, type, hwnd, idObject, idChild, thread, time) =>
            {
                if (hwnd != IntPtr.Zero && 
                    idObject == ObjectIdentifier.OBJID_WINDOW &&
                    idChild == NativeMethods.CHILDID_SELF)
                {
                    AddProcessHook(Windows.Activate(hwnd));
                }
            };
        }

        public ObservableWindowRecordCollection Windows { get; }
        public ConcurrentDictionary<uint, ProcessHook> ActiveHooksByProcessId { get; } = new ConcurrentDictionary<uint, ProcessHook>();

        public void Start()
        {
            var windowRecords = Windows.Initialize();
            foreach (var windowRecord in windowRecords)
            {
                AddProcessHook(windowRecord);
            }
            _activateWindowHook = NativeMethods.SetWinEventHook(EventConstant.EVENT_SYSTEM_FOREGROUND, _activateCallback);
        }

        private void AddProcessHook(WindowRecord windowRecord)
        {
            if (windowRecord == null || windowRecord.ProcessId == 0)
            {
                return;
            }

            if (ActiveHooksByProcessId.ContainsKey(windowRecord.ProcessId))
            {
                // There is already a hook in place for the thread. the thread only spawned a new window
                return;
            }

            var hook = new ProcessHook(windowRecord.ProcessId, Windows);
            hook.ActivateHooks();

            ActiveHooksByProcessId.TryAdd(hook.ProcessId, hook);
            windowRecord.Process.Exited += (sender, args) =>
            {
                ActiveHooksByProcessId.TryRemove(hook.ProcessId, out var removedHook);
                removedHook.Dispose();
            };
        }

        #region Dispose

        private void ReleaseUnmanagedResources()
        {
            NativeMethods.UnhookWinEvent(_activateWindowHook);
            foreach (var activeHookse in ActiveHooksByProcessId)
            {
                activeHookse.Value.Dispose();
            }
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~WindowManager()
        {
            ReleaseUnmanagedResources();
        }

        #endregion

    }
}
