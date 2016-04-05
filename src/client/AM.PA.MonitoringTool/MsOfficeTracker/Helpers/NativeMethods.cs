// Created by André Meyer at MSR
// Created: 2016-01-15
// 
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace MsOfficeTracker.Helpers
{
    internal static class NativeMethods
    {
        [DllImport("wininet.dll", SetLastError = true)]
        internal static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int lpdwBufferLength);
    }
}
