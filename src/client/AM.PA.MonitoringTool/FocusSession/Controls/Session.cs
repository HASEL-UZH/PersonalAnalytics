using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text;
using System.Runtime.InteropServices;

namespace FocusSession.Controls
{
    class Session
    {
        static bool sessionIsActive;

        //code from here https://www.codeproject.com/Articles/824887/How-To-List-The-Name-of-Current-Active-Window-in-C

        // declare prototype of Windows API functions
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        // get the name of the program currently being used by the user (the currently active window)
        public void GetActiveWindow()
        {
            const int nChars = 256;
            IntPtr handle;
            StringBuilder Buff = new StringBuilder(nChars);
            handle = GetForegroundWindow();
            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                Console.WriteLine(Buff.ToString());
            }
        }

    }
}
