// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-07
// 
// Licensed under the MIT License.

using BluetoothLowEnergyConnector;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace BluetoothLowEnergy
{
    
    public delegate void OnNewHeartrateValueEvent(List<HeartRateMeasurement> heartRateMeasurementValue);
    public delegate void OnConnectionToDeviceLost(String deviceName);
    
    public class Connector
    {
        private const string CONTAINER_ID_PROPERTY = "System.Devices.ContainerId";

        private static Connector instance;
        private DateTime timeOFLastDataPoint = DateTime.Now;
        private DeviceInformation connectedDevice;

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

        public async Task<PortableBluetoothDeviceInformation> FindDeviceByName(string name)
        {
            var devices = await GetAllDevices();
            foreach (var device in devices)
            {
                if (device.Name.Equals(name))
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
            connectedDevice = device.Device as DeviceInformation;
            StartWatching();
        }

        private void StartWatching()
        {
            CancellationTokenSource token = new CancellationTokenSource();
            Task perdiodicTask = PeriodicTaskFactory.Start(() =>
            {
                if (timeOFLastDataPoint != null && timeOFLastDataPoint.AddSeconds(10).CompareTo(DateTime.Now) == -1)
                {
                    LoggerWrapper.Instance.WriteToConsole("Received no data since more than 10 seconds");
                    ConnectionLost?.Invoke(connectedDevice.Name);
                    try
                    {
                        token.Cancel();
                    }
                    catch (OperationCanceledException e)
                    {
                        //This is expected!
                        perdiodicTask = null;
                    }
                }

            }, intervalInMilliseconds: 5000, cancelToken: token.Token);

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

        public event OnConnectionToDeviceLost ConnectionLost;

        public event OnNewHeartrateValueEvent ValueChangeCompleted;

        public void Stop()
        {
            HeartRateService.Instance.ValueChangeCompleted -= Instance_ValueChangeCompleted;
        }

        private void Instance_ValueChangeCompleted(List<HeartRateMeasurement> heartRateMeasurementValue)
        {
            timeOFLastDataPoint = DateTime.Now;
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