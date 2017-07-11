using System.Collections.ObjectModel;
using PortableDeviceApiLib;

namespace PortableDevices
{
    public class PortableDeviceCollection : Collection<PortableDevice>
    {
        private readonly PortableDeviceManager _deviceManager;

        public PortableDeviceCollection()
        {
            _deviceManager = new PortableDeviceManager();
        }

        public void Refresh()
        {
            _deviceManager.RefreshDeviceList();

            // Determine how many WPD devices are connected
            var deviceIds = new string[1];
            uint count = 1;
            _deviceManager.GetDevices(ref deviceIds[0], ref count);

            if (count == 0) return;
            // Retrieve the device id for each connected device
            deviceIds = new string[count];
            _deviceManager.GetDevices(ref deviceIds[0], ref count);

            foreach(var deviceId in deviceIds)
            {
                Add(new PortableDevice(deviceId));
            }
        }
    }
}