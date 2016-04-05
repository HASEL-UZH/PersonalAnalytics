// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.

using System;
using Shared.Data;

namespace WindowsContextTracker
{
    public static class Settings
    {
        public static bool IsEnabled = false; // Database.GetInstanceSettings().WindowsContextTrackerEnabled;
        public const string DbTable = "windows_context";
        public const string ScreenshotsSaveFolder = "screenshots\\"; // TEMP

        private const int WindowScreenshotIntervalInSeconds = 120;
        public static TimeSpan WindowScreenshotInterval = TimeSpan.FromSeconds(WindowScreenshotIntervalInSeconds);

        // next study
        public const int NumberOfCharsTypedThreshold = 3 * WindowScreenshotIntervalInSeconds; // 200 keystrokes per minute --> 33 per 10 seconds --> 3.3 per second
        public const int DistanceOfScrollingThreshold = 40 * WindowScreenshotIntervalInSeconds; // 480 scrolling per minute --> 40 scrolling per second
        public const int NumberOfClicksThreshold = (int)0.2 * WindowScreenshotIntervalInSeconds; // 12 clicks per minute --> 0.2 per second

        // set-up during Vancouver-2015 study
        //public const int NumberOfCharsTypedThreshold = 200; // 200 keystrokes per minute --> 33 per 10 seconds --> 3.3 per second
        //public const int DistanceOfScrollingThreshold = 240;
        //public const int NumberOfClicksThreshold = 20;

        // set-up for OCR recognition
        //public const int NumberOfCharsTypedThreshold = (int)3.3 * WindowScreenshotIntervalInSeconds; // 200 keystrokes per minute --> 33 per 10 seconds --> 3.3 per second
        //public const int DistanceOfScrollingThreshold = 240; // or 120
    }
}
