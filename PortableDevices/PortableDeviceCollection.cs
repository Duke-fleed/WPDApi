using System.Collections.ObjectModel;
using System.Runtime.Versioning;
using static Vanara.PInvoke.PortableDeviceApi;

namespace PortableDevices
{
    [SupportedOSPlatform("windows")]
    public class PortableDeviceCollection : Collection<PortableDevice>
    {
        private readonly IPortableDeviceManager _deviceManager;

        public PortableDeviceCollection()
        {
            _deviceManager = (IPortableDeviceManager)new PortableDeviceManager();
        }

        public void Refresh()
        {
            _deviceManager.RefreshDeviceList();

            // Determine how many WPD devices are connected
            uint count = 1;
            var deviceIds = new string[1];
            _deviceManager.GetDevices(deviceIds, ref count);

            if (count == 0) return;

            // Retrieve the device id for each connected device
            deviceIds = new string[count];
            _deviceManager.GetDevices(deviceIds, ref count);

            foreach (var deviceId in deviceIds)
            {
                Add(new PortableDevice(deviceId));
            }
        }
    }
}
