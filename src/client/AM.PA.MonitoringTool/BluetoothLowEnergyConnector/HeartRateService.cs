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
    public delegate void ValueChangeCompletedHandler(List<HeartRateMeasurement> heartRateMeasurementValue);

    public delegate void DeviceConnectionUpdatedHandler(bool isConnected);

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

        public static HeartRateService Instance
        {
            get { return instance; }
        }

        public bool IsServiceInitialized { get; set; }

        public GattDeviceService Service
        {
            get { return service; }
        }

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
        
        public async Task<bool> InitializeServiceAsync(DeviceInformation device)
        {
            try
            {
                deviceContainerId = "{" + device.Properties[ContainerIDProperty] + "}";
                
                service = await GattDeviceService.FromIdAsync(device.Id);
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
                short heartrate = BitConverter.ToInt16(heartRateRecord, offset);
                measurement.HeartRateValue = heartrate;
                offset += 2;
            }
            else //uint8
            {
                byte heartrate = heartRateRecord[offset];
                measurement.HeartRateValue = heartrate;
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
    }
}