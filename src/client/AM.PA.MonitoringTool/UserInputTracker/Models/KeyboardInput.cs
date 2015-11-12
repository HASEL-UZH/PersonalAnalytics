// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.

using System;
using System.Windows.Forms;

namespace UserInputTracker.Models
{
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

        public KeystrokeEvent(KeyEventArgs e)
        {
            Timestamp = DateTime.Now;
            KeystrokeType = GetKeyStrokeType(e.KeyData.ToString());
        }

        /// <summary>
        /// todo: different languages?
        /// </summary>
        /// <param name="stroke"></param>
        /// <returns></returns>
        private static KeystrokeType GetKeyStrokeType(string stroke)
        {
            stroke = stroke.ToLower();
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
            return String.Format("Keystroke: {0}\t {1}", KeystrokeType, Timestamp);
        }
    }
}
