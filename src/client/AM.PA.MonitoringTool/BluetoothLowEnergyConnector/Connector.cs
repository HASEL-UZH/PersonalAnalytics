// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-07
// 
// Licensed under the MIT License.

using BluetoothLowEnergyConnector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace BluetoothLowEnergy
{
    
    public delegate void OnNewHeartrateValueEvent(List<HeartRateMeasurement> heartRateMeasurementValue);
    
    public class Connector
    {
        private const string CONTAINER_ID_PROPERTY = "System.Devices.ContainerId";

        private static Connector instance;

        private Connector() { }

        public async Task<List<PortableBluetoothDeviceInformation>> GetDevices()
        {
            List<PortableBluetoothDeviceInformation> result = new List<PortableBluetoothDeviceInformation>();

            var devices = await GetAllDevices();
            foreach (var device in devices)
            {
                LoggerWrapper.Instance.WriteToConsole(device.Name);
                result.Add(new PortableBluetoothDeviceInformation
                {
                    Id = device.Id,
                    Name = device.Name,
                    Device = device
                }
                );
            }

            return result;
        }

        private async Task<DeviceInformationCollection> GetAllDevices()
        {
            return await DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromUuid(GattServiceUuids.HeartRate), new string[] { CONTAINER_ID_PROPERTY });
        }

        public async Task<PortableBluetoothDeviceInformation> FindDeviceByID(string id)
        {
            var devices = await GetAllDevices();
            foreach (var device in devices)
            {
                if (device.Id.Equals(id))
                {
                    return new PortableBluetoothDeviceInformation
                    {
                        Id = device.Id,
                        Name = device.Name,
                        Device = device
                    };
                }
            }
            return null;
        }

        public async Task Connect(PortableBluetoothDeviceInformation device)
        {
            HeartRateService.Instance.ValueChangeCompleted += Instance_ValueChangeCompleted;
            HeartRateService.Instance.DeviceConnectionUpdated += OnDeviceConnectionUpdated;
            await HeartRateService.Instance.InitializeServiceAsync(device.Device as DeviceInformation);
        }

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

        public void Stop()
        {
            HeartRateService.Instance.ValueChangeCompleted -= Instance_ValueChangeCompleted;
        }

        private void Instance_ValueChangeCompleted(List<HeartRateMeasurement> heartRateMeasurementValue)
        {
            ValueChangeCompleted?.Invoke(heartRateMeasurementValue);
        }

        private void OnDeviceConnectionUpdated(bool isConnected)
        {

            if (isConnected)
            {
                LoggerWrapper.Instance.WriteToConsole("Waiting for device to send data...");
            }
            else
            {
               LoggerWrapper.Instance.WriteToConsole("Waiting for device to connect...");
            }

        }
    }
}