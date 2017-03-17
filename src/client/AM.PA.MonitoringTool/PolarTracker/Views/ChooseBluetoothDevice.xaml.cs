// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-08
// 
// Licensed under the MIT License.

using BluetoothLowEnergy;
using BluetoothLowEnergyConnector;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Shared;
using System;

namespace PolarTracker.Views
{
    /// <summary>
    /// Interaction logic for ChooseBluetoothDevice.xaml
    /// </summary>
    public partial class ChooseBluetoothDevice : Window
    {
        public ChooseBluetoothDevice()
        {
            InitializeComponent();
        }

        private async void FindDevices(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.WriteToConsole("Start looking for Bluetooth devices");
                DeviceList.Visibility = Visibility.Hidden;
                FindButton.IsEnabled = false;

                List<PortableBluetoothDeviceInformation> devices = await Connector.Instance.GetDevices();

                Logger.WriteToConsole("Finsihed looking for Bluetooth devices. Found " + devices.Count + " devices.");

                if (devices.Count > 0)
                {
                    DeviceList.Visibility = Visibility.Visible;
                }

                Devices.Items.Clear();
                foreach (PortableBluetoothDeviceInformation device in devices)
                {
                    Devices.Items.Add(device);
                }
                FindButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Logger.WriteToLogFile(ex);
            }
        }

        public delegate void OnConnectionEstablished(string deviceName);
        public event OnConnectionEstablished ConnectionEstablishedEvent;

        private async void OnDeviceSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FindButton.IsEnabled = false;
            var device = Devices.SelectedItem as PortableBluetoothDeviceInformation;
            await Connector.Instance.Connect(device);

            ConnectionEstablishedEvent?.Invoke(device.Name);
        }

        public delegate void OnTrackerDisabled();
        public event OnTrackerDisabled TrackerDisabledEvent;

        private void DisableTracker(object sender, RoutedEventArgs e)
        {
            TrackerDisabledEvent?.Invoke();
        }
    }
}