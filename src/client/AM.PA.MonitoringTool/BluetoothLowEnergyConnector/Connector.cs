using BluetoothGattHeartRate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace ClassLibrary2
{
    public class Connector
    {
        public async void Start()
        {
            System.Diagnostics.Debug.WriteLine("Start");

            HeartRateService.Instance.ValueChangeCompleted += Instance_ValueChangeCompleted;

            var devices = await DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromUuid(GattServiceUuids.HeartRate), new string[] { "System.Devices.ContainerId" });

            foreach (var d in devices)
            {
                System.Diagnostics.Debug.WriteLine(d.Name);
            }

            HeartRateService.Instance.DeviceConnectionUpdated += OnDeviceConnectionUpdated;
            await HeartRateService.Instance.InitializeServiceAsync(devices[0]);
        }

        private void Instance_ValueChangeCompleted(HeartRateMeasurement heartRateMeasurementValue)
        {

            System.Diagnostics.Debug.WriteLine(heartRateMeasurementValue);

        }

        private void OnDeviceConnectionUpdated(bool isConnected)
        {

            if (isConnected)
            {
                System.Diagnostics.Debug.WriteLine("Waiting for device to send data...");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Waiting for device to connect...");
            }

        }
    }
}
