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
        private DateTime timeOFLastDataPoint = DateTime.MaxValue;
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

        public async Task<bool> Connect(PortableBluetoothDeviceInformation device)
        {
            if (device == null)
            {
                return false;
            }
            else
            {
                return await Connect(device.Device as DeviceInformation);
            }
        }

        private async Task<bool> Connect(DeviceInformation device)
        {
            if (device == null)
            {
                return false;
            }
            else
            {
                HeartRateService.Instance.DeviceConnectionUpdated += OnDeviceConnectionUpdated;

                bool connected = await HeartRateService.Instance.InitializeServiceAsync(device);
                if (connected)
                { 
                    connectedDevice = device;
                    HeartRateService.Instance.ValueChangeCompleted += Instance_ValueChangeCompleted;
                    StartWatching();
                }
                else
                {
                    HeartRateService.Instance.DeviceConnectionUpdated -= OnDeviceConnectionUpdated;
                }
                return connected;
            }
        }

        public void TryReconnectTodevice(DeviceInformation device)
        {
            LoggerWrapper.Instance.WriteToConsole("Should try to reestablish connection to: " + device.Name);

            CancellationTokenSource token = new CancellationTokenSource();
            Task perdiodicTask = PeriodicTaskFactory.Start(async () =>
            {
                LoggerWrapper.Instance.WriteToConsole("Try restablishing connection to " + device.Name);
                bool connected = false;

                if (device != null)
                {
                    //connected = true;
                    connected = await Connect(device);
                }

                if (connected)
                {
                    LoggerWrapper.Instance.WriteToConsole("Connection restablished!");
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
            }, intervalInMilliseconds: 10000, cancelToken: token.Token);
        }

        private void StartWatching()
        {
            CancellationTokenSource token = new CancellationTokenSource();
            Task perdiodicTask = PeriodicTaskFactory.Start(() =>
            {
                if (timeOFLastDataPoint != DateTime.MaxValue && timeOFLastDataPoint.AddSeconds(15).CompareTo(DateTime.Now) == -1)
                {
                    try
                    {
                        token.Cancel();
                    }
                    catch (OperationCanceledException e)
                    {
                        //This is expected!
                        perdiodicTask = null;
                    }
                    HeartRateService.Instance.ValueChangeCompleted -= Instance_ValueChangeCompleted;
                    HeartRateService.Instance.DeviceConnectionUpdated -= OnDeviceConnectionUpdated;
                    timeOFLastDataPoint = DateTime.MaxValue;
                    HeartRateService.Instance.Stop();
                    LoggerWrapper.Instance.WriteToConsole("Received no data since more than 15 seconds");
                    ConnectionLost?.Invoke(connectedDevice.Name);
                    TryReconnectTodevice(connectedDevice);
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