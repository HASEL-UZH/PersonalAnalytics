// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-06
// 
// Licensed under the MIT License.

using BluetoothLowEnergy;
using BluetoothLowEnergyConnector;
using Shared;
using Shared.Data;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System;

namespace PolarTracker.Views
{
    /// <summary>
    /// Interaction logic for FirstStartWindow.xaml
    /// </summary>
    public partial class FirstStartWindow : UserControl, BluetoothDeviceListener
    {
        private ChooseBluetoothDevice chooser;
        
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
    
        internal void NextClicked()
        {
            if (Enable.IsChecked.HasValue)
            {
                if (Enable.IsChecked.Value)
                {
                    chooser = new ChooseBluetoothDevice();
                    chooser.AddListener(this);
                    chooser.ShowDialog();
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

        void BluetoothDeviceListener.OnConnectionEstablished(string deviceName)
        {
            Database.GetInstance().SetSettings(Settings.TRACKER_ENEABLED_SETTING, true);
            Database.GetInstance().SetSettings(Settings.HEARTRATE_TRACKER_ID_SETTING, deviceName);
            chooser.Close();
        }

        void BluetoothDeviceListener.OnTrackerDisabled()
        {
            Database.GetInstance().SetSettings(Settings.TRACKER_ENEABLED_SETTING, false);
            chooser.Close();
        }
    }
}