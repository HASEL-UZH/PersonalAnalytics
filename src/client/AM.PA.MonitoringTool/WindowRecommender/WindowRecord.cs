using System;

namespace WindowRecommender
{
    internal struct WindowRecord
    {
        internal readonly IntPtr Handle;
        internal string Title;
        internal string ProcessName;

        internal WindowRecord(IntPtr windowHandle)
        {
            Handle = windowHandle;
            Title = "";
            ProcessName = "";
        }

        internal WindowRecord(IntPtr windowHandle, string windowTitle, string processName)
        {
            Handle = windowHandle;
            Title = windowTitle;
            ProcessName = processName;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }

            return Handle.Equals(((WindowRecord)obj).Handle);
        }

        public override int GetHashCode()
        {
            return Handle.GetHashCode();
        }
    }
}
