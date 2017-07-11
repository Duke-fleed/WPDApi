using System.Collections.Generic;

namespace PortableDevices
{
    public class PortableDeviceFolder : PortableDeviceObject
    {
        public PortableDeviceFolder(string id, string name) : base(id, name)
        {
            Files = new List<PortableDeviceObject>();
        }

        public IList<PortableDeviceObject> Files { get; set; }
    }
}