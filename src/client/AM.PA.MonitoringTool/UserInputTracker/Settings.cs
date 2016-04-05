// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.

using Shared.Data;
using System;

namespace UserInputTracker
{
    public static class Settings
    {
        public static bool IsEnabledByDefault = true;
        public const int UserInputVisMinutesInterval = 10;

        private const int MouseSnapshotIntervalInSeconds = 1;
        public static TimeSpan MouseSnapshotInterval = TimeSpan.FromSeconds(MouseSnapshotIntervalInSeconds);

        private const int IntervalSaveToDatabaseInSeconds = 30; // or 60 ?
        public static TimeSpan SaveToDatabaseInterval = TimeSpan.FromSeconds(IntervalSaveToDatabaseInSeconds);

        public const string DbTableKeyboard = "user_input_keyboard";
        public const string DbTableMouseClick = "user_input_mouse_click";
        public const string DbTableMouseScrolling = "user_input_mouse_scrolling";
        public const string DbTableMouseMovement = "user_input_mouse_movement";

        // user input level weighting
        // Keystroke Ratio = 1 (base unit)
        public const int MouseClickKeyboardRatio = 3; // todo: more accurate?
        public const double MouseMovementKeyboardRatio = 0.0028; // todo: more accurate?
        public const double MouseScrollingKeyboardRatio = 1.55; // todo: more accurate?
        //public const double MouseScrollingKeyboardRatio = 0.016;
    }
}
