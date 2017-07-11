namespace PortableDevices
{
    public abstract class PortableDeviceObject
    {
        protected PortableDeviceObject(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public string Id { get; }

        public string Name { get; }
    }
}