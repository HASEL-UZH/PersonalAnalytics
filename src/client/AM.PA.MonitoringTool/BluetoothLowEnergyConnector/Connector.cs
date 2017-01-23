// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-07
// 
// Licensed under the MIT License.

using BluetoothLowEnergyConnector;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace BluetoothLowEnergy
{

    //Called when a new heart rate measurement is received from the BLE device
    public delegate void OnNewHeartrateValueEvent(List<HeartRateMeasurement> heartRateMeasurementValue);

    //Caled when the connection to a BLE device is lost. The device's name is passed in the parameter
    public delegate void OnConnectionToDeviceLost(String deviceName);

    //Called when a connection to a BLE device is reastablished after it was lost.
    public delegate void OnConnectionReestablished();

    //Called when the bluetooth functionality is not enabled.
    public delegate void OnBluetoothNotEnabled();


    //This class provides the interface between the BLE functionality and the PolarTracker. The PolarTracker communicates with this class to get data from a BLE device. This class is implemented as a Singleton.
    public class Connector
    {
        private const string CONTAINER_ID_PROPERTY = "System.Devices.ContainerId";

        private static Connector instance;
        private DateTime timeOFLastDataPoint = DateTime.MaxValue;
        private DeviceInformation connectedDevice;

        private Connector() {
            HeartRateService.Instance.BluetoothNotEnabled += OnBluetoothNotEnabled;
        }

        private void OnBluetoothNotEnabled()
        {
            BluetoothNotEnabled?.Invoke();
        }

        //Returns a list of paired BLE devices that are in within reach.
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
                });
            }

            return result;
        }

        //Returns the sensor's location on the body. Possibly locations are: Chest, Wirst, Finger, Hand, Ear Lobe, Foot and Other. It it also possible that the connected device can't provide a body location.
        public async Task<string> GetBodySensorLocation()
        {
            try
            {
                var bodySensorLocationCharacteristics = HeartRateService.Instance.Service.GetCharacteristics(GattCharacteristicUuids.BodySensorLocation);

                if (bodySensorLocationCharacteristics.Count > 0)
                {
                    GattReadResult readResult = await bodySensorLocationCharacteristics[0].ReadValueAsync();
                    if (readResult.Status == GattCommunicationStatus.Success)
                    {
                        byte[] bodySensorLocationData = new byte[readResult.Value.Length];

                        DataReader.FromBuffer(readResult.Value).ReadBytes(bodySensorLocationData);

                        string bodySensorLocation = HeartRateService.Instance.ProcessBodySensorLocationData(bodySensorLocationData);
                        if (bodySensorLocation != "")
                        {
                           return bodySensorLocation;
                        }
                        else
                        {
                            LoggerWrapper.Instance.WriteToConsole("The Body Sensor Location is not recognized");
                        }
                    }
                    else
                    {
                        LoggerWrapper.Instance.WriteToConsole("Device is unreachable, most likely the device is out of range, or is running low on battery");
                    }
                }
                else
                {
                    LoggerWrapper.Instance.WriteToConsole("Device does not support the Body Sensor Location characteristic.");
                }
            }
            catch (Exception e)
            {
                LoggerWrapper.Instance.WriteToLogFile(e);
            }
            return null;
        }

        private async Task<DeviceInformationCollection> GetAllDevices()
        {
            return await DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromUuid(GattServiceUuids.HeartRate), new string[] { CONTAINER_ID_PROPERTY });
        }

        //Checks whether the device passed in the parameter is paired and within reach. If it is, an object representating this device is returned. Otherwise, null is returned.
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

        //Establishes a connection to the device passed as parameter. Returns true if the connection was established and false otherwise.
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

        //Tries to reconnect to the device passed in the parameter.
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
                    ConnectionReestablished?.Invoke();

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
            }, intervalInMilliseconds: Settings.WAIT_TIME_BETWEEN_RECONNECT_TRIES, cancelToken: token.Token);
        }

        private void StartWatching()
        {
            CancellationTokenSource token = new CancellationTokenSource();
            Task perdiodicTask = PeriodicTaskFactory.Start(() =>
            {
                if (timeOFLastDataPoint != DateTime.MaxValue && timeOFLastDataPoint.AddSeconds(Settings.TIME_SINCE_NO_DATA_RECEIVED).CompareTo(DateTime.Now) == -1)
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
                    LoggerWrapper.Instance.WriteToConsole("Received no data since more than " + Settings.TIME_SINCE_NO_DATA_RECEIVED + " seconds");
                    ConnectionLost?.Invoke(connectedDevice.Name);
                    TryReconnectTodevice(connectedDevice);
                }

            }, intervalInMilliseconds: Settings.WAIT_TIME_BETWEEN_WATCHING_THREADS, cancelToken: token.Token);

        }

        //Returns an instance of this class.
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

        public event OnConnectionReestablished ConnectionReestablished;

        public event OnConnectionToDeviceLost ConnectionLost;

        public event OnNewHeartrateValueEvent ValueChangeCompleted;

        public event OnBluetoothNotEnabled BluetoothNotEnabled;

        //Stops receiving data from the connected BLE device
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