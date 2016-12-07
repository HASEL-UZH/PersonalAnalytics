// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-07
// 
// Licensed under the MIT License.

using System;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace BluetoothLowEnergy
{

    public delegate void OnNewHeartrateValueEvent(HeartRateMeasurement heartRateMeasurementValue);

    public class Connector
    {
        private const string CONTAINER_ID_PROPERTY = "System.Devices.ContainerId";

        private static Connector instance;

        private Connector() { }

        public static Connector Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Connector();
                }
                return instance;
            }
        }

        public event OnNewHeartrateValueEvent ValueChangeCompleted;

        public async void Start()
        {
            HeartRateService.Instance.ValueChangeCompleted += Instance_ValueChangeCompleted;

            var devices = await DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromUuid(GattServiceUuids.HeartRate), new string[] { CONTAINER_ID_PROPERTY });
            HeartRateService.Instance.DeviceConnectionUpdated += OnDeviceConnectionUpdated;
            await HeartRateService.Instance.InitializeServiceAsync(devices[0]);
        }

        public async void Stop()
        {
            HeartRateService.Instance.ValueChangeCompleted -= Instance_ValueChangeCompleted;
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