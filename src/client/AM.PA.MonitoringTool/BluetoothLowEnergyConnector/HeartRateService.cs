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
    public delegate void ValueChangeCompletedHandler(HeartRateMeasurement heartRateMeasurementValue);

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
        
        public async Task InitializeServiceAsync(DeviceInformation device)
        {
            try
            {
                deviceContainerId = "{" + device.Properties[ContainerIDProperty] + "}";

                service = await GattDeviceService.FromIdAsync(device.Id);
                if (service != null)
                {
                    IsServiceInitialized = true;
                    await ConfigureServiceForNotificationsAsync();
                }
                else
                {
                   LoggerWrapper.Instance.WriteToConsole("Access to the device is denied, because the application was not granted access, or the device is currently in use by another application.");
                }
            }
            catch (Exception e)
            {
                LoggerWrapper.Instance.WriteToConsole("ERROR: Accessing your device failed." + Environment.NewLine + e.Message);
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
                LoggerWrapper.Instance.WriteToConsole("ERROR: Accessing your device failed." + Environment.NewLine + e.Message);
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

        private void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var data = new byte[args.CharacteristicValue.Length];

            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(data);

            var value = ProcessRawData(data);
            value.Timestamp = args.Timestamp;

            lock (datapoints)
            {
                datapoints.Add(value);
            }

            ValueChangeCompleted?.Invoke(value);
        }

        private HeartRateMeasurement ProcessRawData(byte[] data)
        {
            const byte HEART_RATE_VALUE_FORMAT = 0x01;
         
            byte currentOffset = 0;
            byte flags = data[currentOffset];
            bool isHeartRateValueSizeLong = ((flags & HEART_RATE_VALUE_FORMAT) != 0);
         
            currentOffset++;

            ushort heartRateMeasurementValue = 0;

            if (isHeartRateValueSizeLong)
            {
                heartRateMeasurementValue = (ushort)((data[currentOffset + 1] << 8) + data[currentOffset]);
                currentOffset += 2;
            }
            else
            {
                heartRateMeasurementValue = data[currentOffset];
                currentOffset++;
            }
            
            return new HeartRateMeasurement
            {
                HeartRateValue = heartRateMeasurementValue
            };
        }
    }
}