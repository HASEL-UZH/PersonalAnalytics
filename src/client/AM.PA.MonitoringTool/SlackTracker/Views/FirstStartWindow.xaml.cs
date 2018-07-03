// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-07
// 
// Licensed under the MIT License.

using Shared;
using Shared.Data;
using System.Windows;
using System.Windows.Controls;

namespace SlackTracker.Views
{
    /// <summary>
    /// Interaction logic for FirstStartWindow.xaml
    /// </summary>
    public partial class FirstStartWindow : UserControl, IFirstStartScreen
    {

        public FirstStartWindow()
        {
            InitializeComponent();
            if (Database.GetInstance().HasSetting(Settings.TRACKER_ENEABLED_SETTING))
            {
                Enabled.IsEnabled = Database.GetInstance().GetSettingsBool(Settings.TRACKER_ENEABLED_SETTING, false);
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ThanksMessage.Visibility = Visibility.Visible;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ThanksMessage.Visibility = Visibility.Hidden;
        }

        public void PreviousClicked()
        {
            //not needed
        }

        public void NextClicked()
        {
            //TODO
        }

        private void Browser_RegistrationTokenEvent(string token)
        {
            //TOOD
        }

        private void Browser_FinishEvent()
        {

        }

        public string GetTitle()
        {
            return Settings.TRACKER_NAME;
        }
    }
}