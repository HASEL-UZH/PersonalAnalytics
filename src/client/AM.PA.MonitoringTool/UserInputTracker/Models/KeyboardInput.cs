// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Windows.Forms;

namespace UserInputTracker.Models
{
    /// <summary>
    /// Type to obfuscate the collected keystroke data. Only store the type,
    /// never store the exact keystroke!
    /// </summary>
    public enum KeystrokeType
    {
        Key,
        Backspace,
        Navigate
    }

    public class KeystrokeEvent : IUserInput
    {
        public DateTime Timestamp { get; protected set; }
        public KeystrokeType KeystrokeType { get; private set; }

        /// <summary>
        /// Created per cached keystrokeevent
        /// 
        /// doesn't store the exact keystroke, but the type of the keystroke (KeystrokeType)
        /// </summary>
        /// <param name="e"></param>
        public KeystrokeEvent(KeyEventArgs e)
        {
            Timestamp = DateTime.Now;
            KeystrokeType = GetKeyStrokeType(e.KeyData.ToString());
        }

        /// <summary>
        /// todo: check if this works in other languages
        /// </summary>
        /// <param name="stroke"></param>
        /// <returns></returns>
        private static KeystrokeType GetKeyStrokeType(string stroke)
        {
            stroke = stroke.ToLower(CultureInfo.InvariantCulture);
            if (stroke.Equals("delete") || stroke.Equals("back"))
            {
                return KeystrokeType.Backspace;
            }
            else if (stroke.Equals("left") || stroke.Equals("right") || stroke.Equals("down") || stroke.Equals("up") ||
                     stroke.Equals("pageup") || stroke.Equals("pagedown") || stroke.Equals("home") ||
                     stroke.Equals("next"))
            {
                return KeystrokeType.Navigate;
            }
            else
            {
                return KeystrokeType.Key;
            }
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "Keystroke: {0}, {1}", KeystrokeType, Timestamp);
        }
    }
}
