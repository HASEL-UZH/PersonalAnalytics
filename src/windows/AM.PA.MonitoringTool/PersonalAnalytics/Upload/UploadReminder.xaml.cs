// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2016-05-31
// 
// Licensed under the MIT License.
using System;
using System.Windows;

namespace PersonalAnalytics.Upload
{
    /// <summary>
    /// This pop-up is shown once a week, on the first 
    /// work day of the week
    /// </summary>
    public partial class UploadReminder : Window
    {
        public bool UserSelectedShareData { get; set; }

        public UploadReminder()
        {
            InitializeComponent();
        }

        /// <summary>
        /// override ShowDialog method to place it on the bottom right corner
        /// of the developer's screen
        /// </summary>
        /// <returns></returns>
        public new bool? ShowDialog()
        {
            const int windowWidth = 560; //this.ActualWidth;
            const int windowHeight = 265; //this.ActualHeight;

            this.Topmost = true;
            this.ShowActivated = false;
            this.ShowInTaskbar = false;
            this.ResizeMode = ResizeMode.NoResize;
            //this.Owner = Application.Current.MainWindow;

            this.Closed += this.UploadReminder_OnClosed;

            this.Left = SystemParameters.PrimaryScreenWidth - windowWidth;
            var top = SystemParameters.PrimaryScreenHeight - windowHeight;

            foreach (Window window in Application.Current.Windows)
            {
                var windowName = window.GetType().Name;

                if (!windowName.Equals("DailyProductivityPopUp") || window == this) continue;
                window.Topmost = true;
                top = window.Top - windowHeight;
            }

            this.Top = top;
            return base.ShowDialog();
        }

        /// todo: unsure if still needed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UploadReminder_OnClosed(object sender, EventArgs e)
        {
            foreach (Window window in Application.Current.Windows)
            {
                var windowName = window.GetType().Name;

                if (!windowName.Equals("UploadReminder") || window == this) continue;

                // Adjust any windows that were above this one to drop down
                if (window.Top < this.Top)
                {
                    window.Top = window.Top + this.ActualHeight;
                }
            }
        }

        /// <summary>
        /// Close the pop-up and save the value.
        /// </summary>
        /// <param name="selectedProductivityValue"></param>
        private void UserAnsweredPopUp(bool userAllowsUpload)
        {
            // set responses
            UserSelectedShareData = userAllowsUpload;

            // close window
            DialogResult = true;
            this.Close(); // todo: enable
        }

        private void ShareData(object sender, RoutedEventArgs e)
        {
            UserAnsweredPopUp(true);
        }

        private void NotShareData(object sender, RoutedEventArgs e)
        {
            UserAnsweredPopUp(false);
        }
    }
}
