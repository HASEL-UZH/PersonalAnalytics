// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TaskSwitchTracker.Models;

namespace TaskSwitchTracker
{
    /// <summary>
    /// Interaction logic for UserTaskSwitchesWindow.xaml
    /// </summary>
    public partial class UserTaskSwitchesWindow : Window
    {
        public delegate void NewUserTaskSwitch(TaskSwitch dataInstance);
        public NewUserTaskSwitch NewTaskSwitch { get; set; }

        public void CanBeCalledAnything(TaskSwitch dataInstance)
        {
            // do something with the dataInstance parameter
        }

        public UserTaskSwitchesWindow()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// override ShowDialog method to place it on the bottom right corner
        /// of the developer's screen
        /// </summary>
        /// <returns></returns>
        public new bool? ShowDialog()
        {
            const int windowWidth = Settings.ButtonWindowWidth + 290; //this.ActualWidth;
            const int windowHeight = Settings.ButtonWindowHeight + 5; //this.ActualHeight;

            this.Width = Settings.ButtonWindowWidth;
            this.Height = Settings.ButtonWindowHeight;
            this.WindowStyle = WindowStyle.None;
            this.Background = Brushes.Green;
            this.Topmost = true;
            this.ShowInTaskbar = false;
            //this.MouseDown += Window_MouseDown;
            this.ResizeMode = ResizeMode.NoResize;
            //this.ContextMenu = new ContextMenu(); //TODO
            //this.Owner = Application.Current.MainWindow;

            //this.Closed += this.UserTaskSwitchesWindow_OnClosed;

            this.Left = SystemParameters.PrimaryScreenWidth - windowWidth;
            var top = SystemParameters.PrimaryScreenHeight - windowHeight;

            foreach (Window window in Application.Current.Windows)
            {
                var windowName = window.GetType().Name;

                if (!windowName.Equals("UserTaskwitchesWindow") || window == this) continue;
                window.Topmost = true;
                top = window.Top - windowHeight;
            }

            this.Top = top;
            return base.ShowDialog();
        }

        //private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    if (e.ChangedButton == MouseButton.Left)
        //        this.DragMove();
        //}

        private void TaskSwitchClicked(object sender, RoutedEventArgs e)
        {
            var ts = new TaskSwitch();
            if (NewTaskSwitch != null) NewTaskSwitch(ts);
        }

        ///// <summary>
        ///// todo: unsure if needed
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void UserTaskSwitchesWindow_OnClosed(object sender, EventArgs e)
        //{
        //    foreach (Window window in Application.Current.Windows)
        //    {
        //        var windowName = window.GetType().Name;

        //        if (!windowName.Equals("NotificationWindow") || window == this) continue;
        //        // Adjust any windows that were above this one to drop down
        //        if (window.Top < this.Top)
        //        {
        //            window.Top = window.Top + this.ActualHeight;
        //        }
        //    }
        //}
    }
}
