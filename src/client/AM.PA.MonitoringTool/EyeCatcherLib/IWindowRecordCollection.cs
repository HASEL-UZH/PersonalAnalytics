using System;
using System.Collections.Generic;
using EyeCatcherDatabase;
using EyeCatcherDatabase.Records;

namespace EyeCatcherLib
{
    public interface IWindowRecordCollection : IWindowRecordProvider
    {
        IList<WindowRecord> Initialize();

        WindowRecord Activate(IntPtr hwnd);
        WindowRecord Destroy(IntPtr hwnd);
        WindowRecord MoveOrResize(IntPtr hwnd);
        WindowRecord Minimize(IntPtr hwnd);
        WindowRecord MinimizeEnd(IntPtr hwnd);
        WindowRecord Rename(IntPtr hwnd);

        DesktopRecord GetDesktopRecord();
    }
}