// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-08
// 
// Licensed under the MIT License.

namespace BluetoothLowEnergyConnector
{

    //Wrapper class for DeviceInformation (https://msdn.microsoft.com/library/windows/apps/windows.devices.enumeration.deviceinformation)
    public class PortableBluetoothDeviceInformation
    {
        
        public string Name { get; set; }
        public string Id { get; set; }
        public object Device { get; set; }

        public override string ToString()
        {
            return Name.ToString();
        }

    }
}