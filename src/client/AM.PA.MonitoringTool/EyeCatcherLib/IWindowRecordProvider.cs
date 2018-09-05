using System;
using EyeCatcherDatabase.Records;

namespace EyeCatcherLib
{
    public interface IWindowRecordProvider
    {
        WindowRecord GetWindowRecord(IntPtr hWnd);
    }
}
