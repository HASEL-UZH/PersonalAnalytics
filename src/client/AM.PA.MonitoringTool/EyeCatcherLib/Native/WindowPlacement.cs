using System;
// ReSharper disable InconsistentNaming

namespace EyeCatcherLib.Native
{
    internal struct WindowPlacement
    {
        /// <summary>
        /// The length of the structure, in bytes. Before calling the GetWindowPlacement or SetWindowPlacement functions, set this member to sizeof(WindowPlacement).
        /// </summary>
        public uint length;

        /// <summary>
        /// The flags that control the position of the minimized window and the method by which the window is restored.
        /// </summary>
        public WindowPlacementFlags flags;

        /// <summary>
        /// The current show state of the window.
        /// </summary>
        public ShowWindowCommand showCmd;

        /// <summary>
        /// The coordinates of the window's upper-left corner when the window is minimized.
        /// </summary>
        public POINT ptMinPosition;

        /// <summary>
        /// The coordinates of the window's upper-left corner when the window is maximized.
        /// </summary>
        public POINT ptMaxPosition;

        /// <summary>
        /// The window's coordinates when the window is in the restored position.
        /// </summary>
        public RECT rcNormalPosition;
    }

    [Flags]
    internal enum WindowPlacementFlags : uint
    {
        /// <summary>
        /// If the calling thread and the thread that owns the window are attached to different input queues, 
        /// the system posts the request to the thread that owns the window. This prevents the calling thread 
        /// from blocking its execution while other threads process the request.
        /// </summary>
        WPF_ASYNCWINDOWPLACEMENT = 0x0004,

        /// <summary>
        /// The restored window will be maximized, regardless of whether it was maximized before it was minimized. 
        /// This setting is only valid the next time the window is restored. It does not change the default restoration behavior. 
        /// This flag is only valid when the SW_SHOWMINIMIZED value is specified for the showCmd member.
        /// </summary>
        WPF_RESTORETOMAXIMIZED = 0x0002,

        /// <summary>
        /// The coordinates of the minimized window may be specified. 
        /// This flag must be specified if the coordinates are set in the ptMinPosition member.
        /// </summary>
        WPF_SETMINPOSITION = 0x0001
    }

    internal enum ShowWindowCommand
    {
        SW_HIDE = 0,
        SW_SHOWNORMAL = 1,
        SW_SHOWMINIMIZED = 2,

        /// <summary>
        /// Activates the window and displays it as a maximized window.
        /// </summary>
        SW_SHOWMAXIMIZED = 3,

        /// <summary>
        /// Maximizes the specified window.
        /// </summary>
        SW_MAXIMIZE = 3,
        SW_SHOWNOACTIVATE = 4,
        SW_SHOW = 5,
        SW_MINIMIZE = 6,
        SW_SHOWMINNOACTIVE = 7,
        SW_SHOWNA = 8,
        SW_RESTORE = 9,
    }
}
