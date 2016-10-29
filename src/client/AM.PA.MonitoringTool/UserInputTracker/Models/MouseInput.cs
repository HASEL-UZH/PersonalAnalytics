// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using System;
using System.Globalization;
using System.Windows.Forms;

namespace UserInputTracker.Models
{
    internal abstract class MouseInput : IUserInput
    {
        public DateTime Timestamp { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }

        protected MouseInput(MouseEventArgs e)
        {
            Timestamp = DateTime.Now;
            X = e.X;
            Y = e.Y;
        }
    }

    /// <summary>
    /// Class for the mouse click event
    /// </summary>
    internal class MouseClickEvent : MouseInput
    {
        public MouseButtons Button { get; protected set; }

        public MouseClickEvent(MouseEventArgs e) : base(e)
        {
            Button = e.Button;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "MouseClickEvent: {0}\t{1}\t{2}\t{3}", X, Y, Button, Timestamp);
        }
    }

    /// <summary>
    /// Class for the mouse scroll event.
    /// </summary>
    internal class MouseScrollSnapshot : MouseInput
    {
        public int ScrollDelta { get; set; }

        public MouseScrollSnapshot(MouseEventArgs e) : base(e)
        {
            ScrollDelta = e.Delta;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "MouseScrollSnapshot: {0}\t{1}\t{2}\t{3}", X, Y, ScrollDelta, Timestamp);
        }
    }

    /// <summary>
    /// Class for the mouse input
    /// </summary>
    internal class MouseMovementSnapshot : MouseInput
    {
        public int MovedDistance { get; set; }

        public MouseMovementSnapshot(MouseEventArgs e) : base(e)
        {

        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "MouseMovementSnapshot: {0}\t{1}\t{2}\t{3}", X, Y, MovedDistance, Timestamp);
        }
    }
}
