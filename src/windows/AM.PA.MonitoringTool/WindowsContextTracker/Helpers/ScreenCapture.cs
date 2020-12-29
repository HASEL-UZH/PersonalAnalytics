// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using System.Drawing;
using System;
using System.IO;
using System.Runtime.InteropServices;
using OcrLibrary.Helpers;
using Shared;

namespace WindowsContextTracker.Helpers
{
    public static class ScreenCapture
    {
        /// <summary>
        /// for OCR engine: takes a screenshot and then does some pre-OCR optimizations
        /// and returns it
        /// </summary>
        /// <returns></returns>
        public static Screenshot CaptureActiveWindowHq()
        {
            var screenshot = CaptureWindow();
            //RunOcrPreProcessing(screenshot);
            return screenshot;
        }

        /// <summary>
        /// creates and returns the screenshot of the currently active window
        /// </summary>
        /// <returns></returns>
        private static Screenshot CaptureWindow()
        {
            try
            {
                var ptrHandle = GetForegroundWindow(); // currently active window
                var rect = new Rect();

                //var test = Screen.AllScreens; // => http://stackoverflow.com/questions/3827367/how-can-i-take-a-picture-from-screen (get rect for screen)

                // screen resolution stuff (device-independent units -> Pixels)
                double dpiX;
                double dpiY;
                using (var g = Graphics.FromHwnd(IntPtr.Zero)) // use using to dispose afterwards
                {
                    dpiX = (g.DpiX / 96.0); //TODO: directly here to 300 dpi?
                    dpiY = (g.DpiY / 96.0);
                }

                rect.Right = Convert.ToInt32(rect.Right / dpiX);
                rect.Left = Convert.ToInt32(rect.Left / dpiX);
                rect.Bottom = Convert.ToInt32(rect.Bottom / dpiY);
                rect.Top = Convert.ToInt32(rect.Top / dpiY);

                // get bounds, etc.
                GetWindowRect(ptrHandle, ref rect);
                var width = Math.Abs(rect.Right - rect.Left);
                var height = Math.Abs(rect.Bottom - rect.Top);
                var bounds = new Rectangle(rect.Left, rect.Top, width, height);
                var result = new Bitmap(bounds.Width, bounds.Height);

                // get screenshot
                using (var graphics = Graphics.FromImage(result)) // use using to dispose afterwards
                {
                    graphics.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);
                    graphics.Dispose();
                }

                var sc = new Screenshot(result);
                return sc;
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return null;
        }

        /// <summary>
        /// takes a screenshot, resizes it for later processing and saves it on the participant's computer
        /// hint: this is a temporary solution until OCR works
        /// </summary>
        /// <returns>fileName (to store in database)</returns>
        public static string TakeAndSaveScreenshot()
        {
            // capture screenshot
            var screenshot = CaptureActiveWindowHq();
            if (screenshot == null) return null; // screenshot capture unsuccessful

            // Create log directory if it doesn't already exist
            var basePath = Shared.Settings.ExportFilePath + Settings.ScreenshotsSaveFolder;
            if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);

            // Create subfolder if it doesn't already exist (with date)
            basePath += DateTime.Now.ToShortDateString() + "\\";
            if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);

            // save file
            var fileName = DateTime.Now.Ticks + ".png";
            var filePath = basePath + fileName;
            screenshot.Save(filePath);

            return fileName;
        }

        #region OS-Helpers

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetDesktopWindow();

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

        //[return: MarshalAs(UnmanagedType.Bool)]
        //[DllImport("user32.dll", SetLastError = true)]
        //public static extern bool GetWindowInfo(IntPtr hWnd, ref WINDOWINFO pwi);
        #endregion
    }
}
