// Created by Rohit Kaushik (f20150115@goa.bits-pilani.ac.in) from the University of Zurich
// Created: 2018-07-04
// 
// Licensed under the MIT License.

using SlackTracker.Data;
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
        private Window _browserWindow;

        public FirstStartWindow()
        {
            InitializeComponent();
            if (Database.GetInstance().HasSetting(Settings.TRACKER_ENABLED_SETTING))
            {
                Enabled.IsEnabled = Database.GetInstance().GetSettingsBool(Settings.TRACKER_ENABLED_SETTING, false);
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
            if (Enabled.IsChecked.HasValue)
            {
                if (Enabled.IsChecked.Value)
                {
                    Database.GetInstance().SetSettings(Settings.TRACKER_ENABLED_SETTING, true);

                    var browser = new EmbeddedBrowser(Settings.REGISTRATION_URL);

                    _browserWindow = new Window
                    {
                        Title = Settings.TRACKER_NAME,
                        Content = browser
                    };

                    browser.FinishEvent += Browser_FinishEvent;
                    browser.RegistrationTokenEvent += Browser_RegistrationTokenEvent;
                    _browserWindow.ShowDialog();
                }
                else
                {
                    Database.GetInstance().SetSettings(Settings.TRACKER_ENABLED_SETTING, false);
                    Logger.WriteToConsole("The participant updated the setting '" + Settings.TRACKER_ENABLED_SETTING + "' to False");
                }
            }
            else
            {
                Database.GetInstance().SetSettings(Settings.TRACKER_ENABLED_SETTING, false);
                Logger.WriteToConsole("The participant updated the setting '" + Settings.TRACKER_ENABLED_SETTING + "' to False");
            }
        }

        public string GetTitle()
        {
            return Settings.TRACKER_NAME;
        }

        private void Browser_RegistrationTokenEvent(string token)
        {
            SlackConnector.GetAccessToken(token);
        }

        private void Browser_FinishEvent()
        {
            _browserWindow.Close();
        }
    }
}