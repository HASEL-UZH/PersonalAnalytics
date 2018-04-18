// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2016-04-20
// 
// Licensed under the MIT License.
using System;
using System.Windows;
using System.Windows.Input;

namespace UserEfficiencyTracker
{
    /// <summary>
    /// This pop-up is shown once a day.
    /// Interaction logic for DailyProductivityPopUp.xaml
    /// </summary>
    public partial class DailyProductivityPopUp : Window
    {
        public int UserSelectedProductivity { get; set; }

        public DailyProductivityPopUp(DateTime lastActiveWorkday)
        {
            InitializeComponent();

            // set default values
            if (lastActiveWorkday > DateTime.MinValue)
                LastTimeWorked.Text = " (" + lastActiveWorkday.ToShortDateString() + ")";
        }

        /// <summary>
        /// override ShowDialog method to place it on the bottom right corner
        /// of the developer's screen
        /// </summary>
        /// <returns></returns>
        public new bool? ShowDialog()
        {
            const int windowWidth = 510; //this.ActualWidth;
            const int windowHeight = 295; //this.ActualHeight;

            this.Topmost = true;
            this.ShowActivated = false;
            this.ShowInTaskbar = false;
            this.ResizeMode = ResizeMode.NoResize;
            //this.Owner = Application.Current.MainWindow;

            this.Closed += this.DailyProductivityPopUp_OnClosed;

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

        /// <summary>
        /// todo: unsure if still needed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DailyProductivityPopUp_OnClosed(object sender, EventArgs e)
        {
            foreach (Window window in Application.Current.Windows)
            {
                var windowName = window.GetType().Name;

                if (!windowName.Equals("DailyProductivityPopUp") || window == this) continue;

                // Adjust any windows that were above this one to drop down
                if (window.Top < this.Top)
                {
                    window.Top = window.Top + this.ActualHeight;
                }
            }
        }

        /// <summary>
        /// If the user uses shortcuts to escape or fill out the survey.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnKeyDownHandler(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                UserFinishedSurvey(0); // user didn't respond
            }
            else if (e.Key == Key.D7)
            {
                UserFinishedSurvey(7);
            }
            else if (e.Key == Key.D6)
            {
                UserFinishedSurvey(6);
            }
            else if (e.Key == Key.D5)
            {
                UserFinishedSurvey(5);
            }
            else if (e.Key == Key.D4)
            {
                UserFinishedSurvey(4);
            }
            else if (e.Key == Key.D3)
            {
                UserFinishedSurvey(3);
            }
            else if (e.Key == Key.D2)
            {
                UserFinishedSurvey(2);
            }
            else if (e.Key == Key.D1)
            {
                UserFinishedSurvey(1);
            }
            // else: do nothing
        }

        /// <summary>
        /// Close the pop-up and save the value.
        /// </summary>
        /// <param name="selectedProductivityValue"></param>
        private void UserFinishedSurvey(int selectedProductivityValue)
        {
            // set responses
            UserSelectedProductivity = selectedProductivityValue;

            // close window
            DialogResult = true;
            this.Close(); // todo: enable
        }

        #region Productivity Radio Button

        private void Productivity7_Checked(object sender, RoutedEventArgs e)
        {
            UserFinishedSurvey(7);
        }

        private void Productivity6_Checked(object sender, RoutedEventArgs e)
        {
            UserFinishedSurvey(6);
        }

        private void Productivity5_Checked(object sender, RoutedEventArgs e)
        {
            UserFinishedSurvey(5);
        }

        private void Productivity4_Checked(object sender, RoutedEventArgs e)
        {
            UserFinishedSurvey(4);
        }

        private void Productivity3_Checked(object sender, RoutedEventArgs e)
        {
            UserFinishedSurvey(3);
        }

        private void Productivity2_Checked(object sender, RoutedEventArgs e)
        {
            UserFinishedSurvey(2);
        }

        private void Productivity1_Checked(object sender, RoutedEventArgs e)
        {
            UserFinishedSurvey(1);
        }

        #endregion

        #region Button Events

        private void Postpone0Clicked(object sender, RoutedEventArgs e)
        {
            UserFinishedSurvey(-1); // user didn't work
        }

        private void Postpone3Clicked(object sender, RoutedEventArgs e)
        {
            UserSelectedProductivity = 0; // didn't take it
            DialogResult = true;
            this.Close();
        }

        #endregion
    }
}
