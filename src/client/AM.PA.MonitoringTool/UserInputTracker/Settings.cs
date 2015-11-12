// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.

using System;

namespace UserInputTracker
{
    public static class Settings
    {
        public const bool IsEnabled = true;

        private const int MouseSnapshotIntervalInSeconds = 1;
        public static TimeSpan MouseSnapshotInterval = TimeSpan.FromSeconds(MouseSnapshotIntervalInSeconds);

        private const int IntervalSaveToDatabaseInSeconds = 30; // or 60 ?
        public static TimeSpan SaveToDatabaseInterval = TimeSpan.FromSeconds(IntervalSaveToDatabaseInSeconds);

        public const string DbTableKeyboard = "user_input_keyboard";
        public const string DbTableMouseClick = "user_input_mouse_click";
        public const string DbTableMouseScrolling = "user_input_mouse_scrolling";
        public const string DbTableMouseMovement = "user_input_mouse_movement";
    }
}
