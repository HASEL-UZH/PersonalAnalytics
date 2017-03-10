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

        private static Connector _instance;
        private DateTime _timeOFLastDataPoint = DateTime.MaxValue;
        private DeviceInformation _connectedDevice;

        private Timer _watchTimer;
        private Timer _reconnectTimer;

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

            try
            {
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
            }
            catch (Exception e)
            {
                LoggerWrapper.Instance.WriteToLogFile(e);
            }

            return result;
        }

        //Returns the sensor's location on the body. Possibly locations are: Chest, Wirst, Finger, Hand, Ear Lobe, Foot and Other. It it also possible that the connected device can't provide a body location.
        //public async Task<string> GetBodySensorLocation()
        //{
        //    try
        //    {
        //        var bodySensorLocationCharacteristics = HeartRateService.Instance.Service.GetCharacteristics(GattCharacteristicUuids.BodySensorLocation);

        //        if (bodySensorLocationCharacteristics.Count > 0)
        //        {
        //            GattReadResult readResult = await bodySensorLocationCharacteristics[0].ReadValueAsync();
        //            if (readResult.Status == GattCommunicationStatus.Success)
        //            {
        //                byte[] bodySensorLocationData = new byte[readResult.Value.Length];

        //                DataReader.FromBuffer(readResult.Value).ReadBytes(bodySensorLocationData);

        //                string bodySensorLocation = HeartRateService.Instance.ProcessBodySensorLocationData(bodySensorLocationData);
        //                if (bodySensorLocation != "")
        //                {
        //                   return bodySensorLocation;
        //                }
        //                else
        //                {
        //                    LoggerWrapper.Instance.WriteToConsole("The Body Sensor Location is not recognized");
        //                }
        //            }
        //            else
        //            {
        //                LoggerWrapper.Instance.WriteToConsole("Device is unreachable, most likely the device is out of range, or is running low on battery");
        //            }
        //        }
        //        else
        //        {
        //            LoggerWrapper.Instance.WriteToConsole("Device does not support the Body Sensor Location characteristic.");
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        LoggerWrapper.Instance.WriteToLogFile(e);
        //    }
        //    return null;
        //}

        private async Task<DeviceInformationCollection> GetAllDevices()
        {
            try
            {
                return await DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromUuid(GattServiceUuids.HeartRate), new string[] { CONTAINER_ID_PROPERTY });
            }
            catch (Exception e)
            {
                LoggerWrapper.Instance.WriteToLogFile(e);
            }

            return null;
        }

        //Checks whether the device passed in the parameter is paired and within reach. If it is, an object representating this device is returned. Otherwise, null is returned.
        public async Task<PortableBluetoothDeviceInformation> FindDeviceByName(string name)
        {
            try
            {
                var devices = await GetAllDevices();
                if (devices != null)
                {
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
                }
            }
            catch (Exception e)
            {
                LoggerWrapper.Instance.WriteToLogFile(e);
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
            try
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
                        _connectedDevice = device;
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
            catch (Exception e)
            {
                LoggerWrapper.Instance.WriteToConsole(e.ToString());
                return false;
            }
        }
        
        //Tries to reconnect to the device passed in the parameter.
        public void TryReconnectToDevice(DeviceInformation device)
        {
            LoggerWrapper.Instance.WriteToConsole("Should try to reestablish connection to: " + device.Name);
            _reconnectTimer = new Timer(OnReconnectTry, device, 0, Settings.WAIT_TIME_BETWEEN_RECONNECT_TRIES);
        }

        private async void OnReconnectTry(object obj)
        {
            DeviceInformation device = obj as DeviceInformation;

            LoggerWrapper.Instance.WriteToConsole("Try restablishing connection to " + device.Name);

            bool connected = false;

            if (device != null)
            {
                connected = await Connect(device);
            }

            if (connected)
            {
                if (_reconnectTimer != null)
                {
                    _reconnectTimer.Cancel();
                }
                ConnectionReestablished?.Invoke();
            }
        }
        
        private void StartWatching()
        {
            _watchTimer = new Timer(OnWatchPeriodPassed, null, 0, Settings.WAIT_TIME_BETWEEN_WATCHING_THREADS);
        }

        private void OnWatchPeriodPassed(object state)
        {
            LoggerWrapper.Instance.WriteToConsole("Watching BLE device");
            if (_timeOFLastDataPoint != DateTime.MaxValue && _timeOFLastDataPoint.AddSeconds(Settings.TIME_SINCE_NO_DATA_RECEIVED).CompareTo(DateTime.Now) == -1)
            {
                if (_watchTimer != null)
                {
                    _watchTimer.Cancel();
                }
                HeartRateService.Instance.ValueChangeCompleted -= Instance_ValueChangeCompleted;
                HeartRateService.Instance.DeviceConnectionUpdated -= OnDeviceConnectionUpdated;
                _timeOFLastDataPoint = DateTime.MaxValue;
                HeartRateService.Instance.Stop();
                LoggerWrapper.Instance.WriteToConsole("Received no data since more than " + Settings.TIME_SINCE_NO_DATA_RECEIVED + " seconds");
                ConnectionLost?.Invoke(_connectedDevice.Name);
                TryReconnectToDevice(_connectedDevice);
            }
        }

        //Returns an instance of this class.
        public static Connector Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Connector();
                }
                return _instance;
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
            _timeOFLastDataPoint = DateTime.Now;
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