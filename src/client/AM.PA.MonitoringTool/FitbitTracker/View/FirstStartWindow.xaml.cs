// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-07
// 
// Licensed under the MIT License.

using Shared.Data;
using System.Windows;

namespace FitbitTracker.View
{
    /// <summary>
    /// Interaction logic for FirstStartWindow.xaml
    /// </summary>
    public partial class FirstStartWindow : Window
    {
        private string token;

        //Called when new tokens were received
        public delegate void OnRegistrationToken(string token);
        public event OnRegistrationToken RegistrationTokenEvent;

        //Called when an error happened during retrieving new tokens
        public delegate void OnError();
        public event OnError ErrorEvent;

        public FirstStartWindow()
        {
            InitializeComponent();
            Browser.ErrorEvent += Browser_ErrorEvent;
            Browser.RegistrationTokenEvent += Browser_RegistrationTokenEvent;
        }

        private void Browser_RegistrationTokenEvent(string token)
        {
            this.token = token;
            OK.IsEnabled = true;
            Close.IsEnabled = false;
        }

        private void Browser_ErrorEvent()
        {
            OK.IsEnabled = false;
            Close.IsEnabled = true;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ThanksMessage.Visibility = Visibility.Visible;
            Browser.Visibility = Visibility.Visible;
            Browser.Navigate(Settings.REGISTRATION_URL);
            Close.IsEnabled = false;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ThanksMessage.Visibility = Visibility.Hidden;
            Browser.Visibility = Visibility.Hidden;
            Close.IsEnabled = true;
            OK.IsEnabled = false;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Database.GetInstance().SetSettings(Settings.TRACKER_ENEABLED_SETTING, false);
            ErrorEvent?.Invoke();
            this.Close();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            Database.GetInstance().SetSettings(Settings.TRACKER_ENEABLED_SETTING, true);
            RegistrationTokenEvent?.Invoke(token);
            this.Close();
        }

    }
}