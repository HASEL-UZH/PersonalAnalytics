// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-07
// 
// Licensed under the MIT License.
//
// Adapted from: https://code.msdn.microsoft.com/windowsapps/Bluetooth-Generic-5a99ef95/view/SourceCode#content

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Devices.Enumeration.Pnp;

namespace BluetoothLowEnergy
{
    //Called when a new measurement is received
    public delegate void ValueChangeCompletedHandler(List<HeartRateMeasurement> heartRateMeasurementValue);

    //Called when the connection status to the device changes
    public delegate void DeviceConnectionUpdatedHandler(bool isConnected);

    //Called when bluetooth is not enabled
    public delegate void BluetoothNotEnabledHandler();


    //This class is responsible for receiveing and parsing data from the BLE device and passing it then to the Connector. Implemented as a Singleton.
    public class HeartRateService
    {
        private const int CHARACTERISTIC_INDEX = 0;
        private const GattClientCharacteristicConfigurationDescriptorValue CHARACTERISTIC_NOTIFICATION_TYPE = GattClientCharacteristicConfigurationDescriptorValue.Notify;
        private const string ConnectedProperty = "System.Devices.Connected";
        private const string ContainerIDProperty = "System.Devices.ContainerId";
        private static HeartRateService instance = new HeartRateService();

        private Guid CHARACTERISTIC_UUID = GattCharacteristicUuids.HeartRateMeasurement;
        private GattDeviceService service;
        private GattCharacteristic characteristic;
        private List<HeartRateMeasurement> datapoints;
        private PnpObjectWatcher watcher;
        private String deviceContainerId;

        public event ValueChangeCompletedHandler ValueChangeCompleted;
        public event DeviceConnectionUpdatedHandler DeviceConnectionUpdated;
        public event BluetoothNotEnabledHandler BluetoothNotEnabled;

        //Returns the instance of this class.
        public static HeartRateService Instance
        {
            get { return instance; }
        }

        //Returns a bool indicating whether this service is running or not
        public bool IsServiceInitialized { get; set; }

        //Returns a reference to the Gatt service
        public GattDeviceService Service
        {
            get { return service; }
        }

        //Returns a list of datapoints received from the BLE device
        public HeartRateMeasurement[] DataPoints
        {
            get
            {
                HeartRateMeasurement[] retval;
                lock (datapoints)
                {
                    retval = datapoints.ToArray();
                }
                return retval;
            }
        }

        private HeartRateService()
        {
            datapoints = new List<HeartRateMeasurement>();
        }
        
        //Starts the service. Returns true if started sucessfully and false otherwise.
        public async Task<bool> InitializeServiceAsync(DeviceInformation device)
        {
            try
            {
                deviceContainerId = "{" + device.Properties[ContainerIDProperty] + "}";
                
                try
                {
                    service = await GattDeviceService.FromIdAsync(device.Id);
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("Bluetooth radio is required and it must be enabled"))
                    {
                        BluetoothNotEnabled?.Invoke();
                        return false;
                    }
                }

                if (service != null)
                {
                    IsServiceInitialized = true;
                    await ConfigureServiceForNotificationsAsync();
                    return true;
                }
                else
                {
                   LoggerWrapper.Instance.WriteToConsole("Access to the device is denied, because the application was not granted access, or the device is currently in use by another application.");
                   return false;
                }
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private async Task ConfigureServiceForNotificationsAsync()
        {
            try
            {
                characteristic = service.GetCharacteristics(CHARACTERISTIC_UUID)[CHARACTERISTIC_INDEX];
                characteristic.ProtectionLevel = GattProtectionLevel.EncryptionRequired;
                characteristic.ValueChanged += Characteristic_ValueChanged;

                var currentDescriptorValue = await characteristic.ReadClientCharacteristicConfigurationDescriptorAsync();

                if ((currentDescriptorValue.Status != GattCommunicationStatus.Success) || (currentDescriptorValue.ClientCharacteristicConfigurationDescriptor != CHARACTERISTIC_NOTIFICATION_TYPE))
                {
                    GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(CHARACTERISTIC_NOTIFICATION_TYPE);
                    if (status == GattCommunicationStatus.Unreachable)
                    {
                        StartDeviceConnectionWatcher();
                    }
                }
            }
            catch (Exception e)
            {
                LoggerWrapper.Instance.WriteToLogFile(e);
            }
        }

        private void StartDeviceConnectionWatcher()
        {
            watcher = PnpObject.CreateWatcher(PnpObjectType.DeviceContainer, new string[] { ConnectedProperty }, String.Empty);
            watcher.Updated += DeviceConnection_Updated;
            watcher.Start();
        }

        private async void DeviceConnection_Updated(PnpObjectWatcher sender, PnpObjectUpdate args)
        {
            var connectedProperty = args.Properties[ConnectedProperty];
            bool isConnected = false;
            if ((deviceContainerId == args.Id) && Boolean.TryParse(connectedProperty.ToString(), out isConnected) && isConnected)
            {
                var status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(CHARACTERISTIC_NOTIFICATION_TYPE);

                if (status == GattCommunicationStatus.Success)
                {
                    IsServiceInitialized = true;
                    watcher.Stop();
                    watcher = null;
                }
                DeviceConnectionUpdated?.Invoke(isConnected);
            }
        }

        //Stops the service
        internal void Stop()
        {
            IsServiceInitialized = false;
            datapoints.Clear();
            if (service != null)
            {
                service.Dispose();
            }

            if (characteristic != null)
            {
                characteristic = null;
            }

            if (watcher != null)
            {
                watcher.Stop();
                watcher = null;
            }
        }

        private void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var data = new byte[args.CharacteristicValue.Length];

            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(data);

            List<HeartRateMeasurement> values = ProcessRawData(data);
           
            lock (datapoints)
            {
                foreach (HeartRateMeasurement value in values)
                {
                    datapoints.Add(value);
                }
            }

            ValueChangeCompleted?.Invoke(values);
        }

        private List<HeartRateMeasurement> ProcessRawData(byte[] heartRateRecord)
        {
            //Parse the raw byte stream and extract the HR/EE/HRV values. For reference see: https://www.bluetooth.com/specifications/gatt/viewer?attributeXmlFile=org.bluetooth.characteristic.heart_rate_measurement.xml&u=org.bluetooth.characteristic.heart_rate_measurement.xml

            List<HeartRateMeasurement> measurements = new List<HeartRateMeasurement>();
            HeartRateMeasurement measurement = new HeartRateMeasurement();
            
            byte flags = heartRateRecord[0];
            ushort offset = 1;

            //Heart rate
            bool longHeartRate = (flags & 1) == 1;
            if (longHeartRate) //uint16
            {
                double heartrate = BitConverter.ToDouble(heartRateRecord, offset);
                measurement.HeartRateValue = heartrate;
                offset += 2;
            }
            else //uint8
            {
                byte heartrate = heartRateRecord[offset];
                measurement.HeartRateValue = (double) heartrate;
                offset += 1;
            }

            //Energy Expended
            bool hasEEValue = (flags & (1 << 3)) != 0;
            if (hasEEValue)
            {
                //The Polar 7 does not support EE values, so we don't store the value and just increase the offset by 2
                byte energyValue = heartRateRecord[offset];
                offset += 2;
            }

            //RR interval
            bool hasRRValue = (flags & (1 << 4)) != 0;
            if (hasRRValue)
            {
                int count = (heartRateRecord.Length - offset) / 2;
                //there can be more than one RR Interval in a single message;
                for (int i = 0; i < count; i++)
                {
                    ushort value = BitConverter.ToUInt16(heartRateRecord, offset);
                    double intervalLengthInSeconds = value / 1024.0;
                    measurement.RRInterval = intervalLengthInSeconds;
                    measurement.Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    measurements.Add(measurement);
                    offset += 2;
                    measurement = new HeartRateMeasurement();
                }
            }
            return measurements;
        }

        //Returns the sensor's location on the body
        public string ProcessBodySensorLocationData(byte[] bodySensorLocationData)
        {
            byte bodySensorLocationValue = bodySensorLocationData[0];
            string value = string.Empty;

            switch (bodySensorLocationValue)
            {
                case 0:
                    value += "Other";
                    break;
                case 1:
                    value += "Chest";
                    break;
                case 2:
                    value += "Wrist";
                    break;
                case 3:
                    value += "Finger";
                    break;
                case 4:
                    value += "Hand";
                    break;
                case 5:
                    value += "Ear Lobe";
                    break;
                case 6:
                    value += "Foot";
                    break;
                default:
                    value = "";
                    break;
            }
            return value;
        }

    }
}