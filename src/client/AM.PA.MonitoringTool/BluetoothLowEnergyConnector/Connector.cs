// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-07
// 
// Licensed under the MIT License.

using System;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace BluetoothLowEnergy
{

    public delegate void NewHeartrateValueEvent(HeartRateMeasurement heartRateMeasurementValue);

    public class Connector
    {
        
        public event NewHeartrateValueEvent ValueChangeCompleted;

        public async void Start()
        {
            Logger.WriteToConsole("Start");

            HeartRateService.Instance.ValueChangeCompleted += Instance_ValueChangeCompleted;

            var devices = await DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromUuid(GattServiceUuids.HeartRate), new string[] { "System.Devices.ContainerId" });

            foreach (var d in devices)
            {
                Logger.WriteToConsole(d.Name);
            }

            HeartRateService.Instance.DeviceConnectionUpdated += OnDeviceConnectionUpdated;
            await HeartRateService.Instance.InitializeServiceAsync(devices[0]);
        }

        private void Instance_ValueChangeCompleted(HeartRateMeasurement heartRateMeasurementValue)
        {
            ValueChangeCompleted?.Invoke(heartRateMeasurementValue);
        }

        private void OnDeviceConnectionUpdated(bool isConnected)
        {

            if (isConnected)
            {
                Logger.WriteToConsole("Waiting for device to send data...");
            }
            else
            {
               Logger.WriteToConsole("Waiting for device to connect...");
            }

        }
    }
}
