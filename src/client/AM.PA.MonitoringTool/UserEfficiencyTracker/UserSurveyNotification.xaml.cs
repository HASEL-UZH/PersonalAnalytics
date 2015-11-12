// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using System;
using System.Windows;
using System.Windows.Threading;

namespace UserEfficiencyTracker
{
    public enum PostPoneSurvey
    {
        None,
        PostponeShort,
        PostponeDay,
        PostponeHour
    };

    /// <summary>
    /// Interaction logic for UserSurveyNotification.xaml
    /// </summary>
    public partial class UserSurveyNotification : Window
    {
        public bool TakeSurveyNow { get; set; }
        public PostPoneSurvey PostPoneSurvey { get; set; }

        public UserSurveyNotification()
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
            const int windowWidth = 320; //this.ActualWidth;
            const int windowHeight = 230; //this.ActualHeight;

            this.Topmost = true;
            this.ShowActivated = false;
            this.ShowInTaskbar = false;
            this.ResizeMode = ResizeMode.NoResize;
            //this.Owner = Application.Current.MainWindow;
            
            this.Closed += this.UserSurveyNotification_OnClosed;

            this.Left = SystemParameters.PrimaryScreenWidth - windowWidth;
            var top = SystemParameters.PrimaryScreenHeight - windowHeight;

            foreach (Window window in Application.Current.Windows)
            {
                var windowName = window.GetType().Name;

                if (!windowName.Equals("UserSurveyNotification") || window == this) continue;
                window.Topmost = true;
                top = window.Top - windowHeight;
            }

            this.Top = top;
            return base.ShowDialog();
        }

        /// <summary>
        /// todo: unsure if needed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserSurveyNotification_OnClosed(object sender, EventArgs e)
        {
            foreach (Window window in Application.Current.Windows)
            {
                var windowName = window.GetType().Name;

                if (!windowName.Equals("NotificationWindow") || window == this) continue;
                // Adjust any windows that were above this one to drop down
                if (window.Top < this.Top)
                {
                    window.Top = window.Top + this.ActualHeight;
                }
            }
        }

        /// <summary>
        /// user clicks to take the survey now
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunSurveyClicked(object sender, RoutedEventArgs e)
        {
            TakeSurveyNow = true;
            PostPoneSurvey = PostPoneSurvey.None;
            DialogResult = true;
            this.Close();
        }

        /// <summary>
        /// user postpones the survey for a couple of minutes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PostponeShortSurveyClicked(object sender, RoutedEventArgs e)
        {
            TakeSurveyNow = false;
            PostPoneSurvey = PostPoneSurvey.PostponeShort;
            DialogResult = true;
            this.Close();
        }

        /// <summary>
        /// user postpones the survey for one day
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PostponeDaySurveyClicked(object sender, RoutedEventArgs e)
        {
            TakeSurveyNow = false;
            PostPoneSurvey = PostPoneSurvey.PostponeDay;
            DialogResult = true;
            this.Close();
        }

        /// <summary>
        /// user postpones the survey for one week
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PostponeHourSurveyClicked(object sender, RoutedEventArgs e)
        {
            TakeSurveyNow = false;
            PostPoneSurvey = PostPoneSurvey.PostponeHour;
            DialogResult = true;
            this.Close();
        }
    }
}