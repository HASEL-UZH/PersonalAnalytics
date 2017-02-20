// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-07
// 
// Licensed under the MIT License.

using FitbitTracker.Data;
using Shared.Data;
using System;
using System.Windows;
using System.Windows.Controls;

namespace FitbitTracker.View
{
    /// <summary>
    /// Interaction logic for FirstStartWindow.xaml
    /// </summary>
    public partial class FirstStartWindow : UserControl
    {

        private Window browserWindow;

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

        internal void NextClicked()
        {
            if (Enabled.IsChecked.HasValue)
            {

                if (Enabled.IsChecked.Value)
                {
                    Database.GetInstance().SetSettings(Settings.TRACKER_ENEABLED_SETTING, true);
                    
                    EmbeddedBrowser browser = new EmbeddedBrowser(Settings.REGISTRATION_URL);

                    browserWindow = new Window
                    {
                        Title = "Register PersonalAnalytics to let it access Fitbit data",
                        Content = browser
                    };

                    browser.FinishEvent += Browser_FinishEvent;
                    browser.RegistrationTokenEvent += Browser_RegistrationTokenEvent;
                    browserWindow.ShowDialog();
                }
                else
                {
                    Database.GetInstance().SetSettings(Settings.TRACKER_ENEABLED_SETTING, false);
                }
            }
            else
            {
                Database.GetInstance().SetSettings(Settings.TRACKER_ENEABLED_SETTING, false);
            }
        }

        private void Browser_RegistrationTokenEvent(string token)
        {
            FitbitConnector.GetFirstAccessToken(token);
        }

        private void Browser_FinishEvent()
        {
            browserWindow.Close();
        }
    }
}