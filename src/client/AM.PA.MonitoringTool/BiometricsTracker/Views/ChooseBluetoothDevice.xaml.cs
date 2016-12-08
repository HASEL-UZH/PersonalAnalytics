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

namespace BiometricsTracker.Views
{
    /// <summary>
    /// Interaction logic for ChooseBluetoothDevice.xaml
    /// </summary>
    public partial class ChooseBluetoothDevice : UserControl
    {
        public ChooseBluetoothDevice()
        {
            InitializeComponent();
        }

        private async void FindDevices(object sender, RoutedEventArgs e)
        {
            Shared.Logger.WriteToConsole("Start looking for Bluetooth devices");
            FindButton.IsEnabled = false;

            List<PortableBluetoothDeviceInformation> devices = await Connector.Instance.GetDevices();

            Shared.Logger.WriteToConsole("Finsihed looking for Bluetooth devices. Found " + devices.Count + " devices.");

            Devices.Items.Clear();
            foreach (PortableBluetoothDeviceInformation device in devices)
            {
                Devices.Items.Add(device);
            }
            FindButton.IsEnabled = true;
        }

        private List<Deamon> listeners = new List<Deamon>();

        internal void AddListener(Deamon deamon)
        {
            if (!listeners.Contains(deamon))
            {
                listeners.Add(deamon);
            }
        }

        private async void OnDeviceSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FindButton.IsEnabled = false;
            var device = Devices.SelectedItem as PortableBluetoothDeviceInformation;
            await Connector.Instance.Connect(device);
            
            foreach (var listener in listeners)
            {
                listener.OnConnectionEstablished(device.Id);
            }
        }

        private void DisableTracker(object sender, RoutedEventArgs e)
        {
            foreach (var listener in listeners)
            {
                listener.OnTrackerDisabled();
            }
        }
    }
}