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

namespace PolarTracker.Views
{
    /// <summary>
    /// Interaction logic for FirstStartWindow.xaml
    /// </summary>
    public partial class FirstStartWindow : Window
    {

        private PortableBluetoothDeviceInformation device;

        public FirstStartWindow()
        {
            InitializeComponent();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            FindDevices();

            ThanksMessage.Visibility = Visibility.Visible;
            DeviceList.Visibility = Visibility.Visible;
            Refresh.Visibility = Visibility.Visible;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ThanksMessage.Visibility = Visibility.Hidden;
            DeviceList.Visibility = Visibility.Hidden;
            Refresh.Visibility = Visibility.Hidden;
            Choose.Visibility = Visibility.Hidden;
        }

        private async void FindDevices()
        {
            Logger.WriteToConsole("Start looking for Bluetooth devices");
            
            List<PortableBluetoothDeviceInformation> devices = await Connector.Instance.GetDevices();

            Logger.WriteToConsole("Finsihed looking for Bluetooth devices. Found " + devices.Count + " devices.");
            
            Devices.Items.Clear();
            foreach (PortableBluetoothDeviceInformation device in devices)
            {
                Devices.Items.Add(device);
            }
        }

        private void OnDeviceSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Choose.Visibility = Visibility.Visible;
            device = Devices.SelectedItem as PortableBluetoothDeviceInformation;
        }

        private void Choose_Click(object sender, RoutedEventArgs e)
        {
            LeaveWithEnabling();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            Choose.Visibility = Visibility.Hidden;
            device = null;
            Devices.Items.Clear();
            FindDevices();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            LeaveWithouthEnabling();
        }

        private void LeaveWithouthEnabling()
        {
            Database.GetInstance().LogInfo("The participant updated the setting '" + Settings.TRACKER_ENEABLED_SETTING + "' to False");
            Database.GetInstance().SetSettings(Settings.TRACKER_ENEABLED_SETTING, false);
            Logger.WriteToConsole("The participant updated the setting '" + Settings.TRACKER_ENEABLED_SETTING + "' to False");

            this.Close();
        }

        private void LeaveWithEnabling()
        {
            Database.GetInstance().LogInfo("The participant updated the setting '" + Settings.TRACKER_ENEABLED_SETTING + "' to True");
            Database.GetInstance().SetSettings(Settings.TRACKER_ENEABLED_SETTING, true);
            device = Devices.SelectedItem as PortableBluetoothDeviceInformation;
            Database.GetInstance().SetSettings(Settings.HEARTRATE_TRACKER_ID_SETTING, device.Name);

            Logger.WriteToConsole("The participant updated the setting '" + Settings.TRACKER_ENEABLED_SETTING + "' to True. Choosen device: " + device.Name);
            this.Close();
        }
    }
}
