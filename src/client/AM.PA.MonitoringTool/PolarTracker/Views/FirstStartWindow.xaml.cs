// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-06
// 
// Licensed under the MIT License.

using Shared;
using Shared.Data;
using System.Windows;
using System.Windows.Controls;

namespace PolarTracker.Views
{
    /// <summary>
    /// Interaction logic for FirstStartWindow.xaml
    /// </summary>
    public partial class FirstStartWindow : UserControl, IFirstStartScreen
    {
        private ChooseBluetoothDevice _chooser;
        
        public FirstStartWindow()
        {
            InitializeComponent();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ThanksMessage.Visibility = Visibility.Visible;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ThanksMessage.Visibility = Visibility.Collapsed;
        }
    
        public string GetTitle()
        {
            return Settings.Name;
        }

        public void NextClicked()
        {
            if (Enable.IsChecked.HasValue)
            {
                if (Enable.IsChecked.Value)
                {
                    _chooser = new ChooseBluetoothDevice();
                    _chooser.ConnectionEstablishedEvent += OnConnectionEstablished;
                    _chooser.TrackerDisabledEvent += OnTrackerDisabled;
                    _chooser.ShowDialog();
                }
                else
                {
                    Database.GetInstance().SetSettings(Settings.TRACKER_ENEABLED_SETTING, false);
                    Logger.WriteToConsole("The participant updated the setting '" + Settings.TRACKER_ENEABLED_SETTING + "' to False");
                }
            }
            else
            {
                Database.GetInstance().SetSettings(Settings.TRACKER_ENEABLED_SETTING, false);
                Logger.WriteToConsole("The participant updated the setting '" + Settings.TRACKER_ENEABLED_SETTING + "' to False");
            }
        }

        public void PreviousClicked()
        {

        }

        void OnConnectionEstablished(string deviceName)
        {
            Database.GetInstance().SetSettings(Settings.TRACKER_ENEABLED_SETTING, true);
            Database.GetInstance().SetSettings(Settings.HEARTRATE_TRACKER_ID_SETTING, deviceName);
            _chooser.Close();
        }

        void OnTrackerDisabled()
        {
            Database.GetInstance().SetSettings(Settings.TRACKER_ENEABLED_SETTING, false);
            _chooser.Close();
        }
    }
}