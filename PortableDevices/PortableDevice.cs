using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;
using Vanara.PInvoke;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.PortableDeviceApi;

// Alias to avoid conflict with Vanara.PInvoke.PortableDeviceApi.PortableDevice coclass
using VanaraPortableDevice = Vanara.PInvoke.PortableDeviceApi.PortableDevice;

namespace PortableDevices
{
    [SupportedOSPlatform("windows")]
    public class PortableDevice
    {
        private bool _isConnected;
        private readonly IPortableDevice _device;

        public PortableDevice(string deviceId)
        {
            _device = (IPortableDevice)new VanaraPortableDevice();
            DeviceId = deviceId;
        }

        public string DeviceId { get; set; }

        public string FriendlyName
        {
            get
            {
                if (!_isConnected)
                    throw new InvalidOperationException("Not connected to device.");

                var content = _device.Content();
                var properties = content.Properties();
                var propertyValues = properties.GetValues("DEVICE", null);

                var property = new PROPERTYKEY(
                    new Guid(0x26D4979A, 0xE643, 0x4626, 0x9E, 0x2B, 0x73, 0x6D, 0xC0, 0xC9, 0x2F, 0xDC),
                    12);

                return propertyValues.GetStringValue(in property);
            }
        }

        public void Connect()
        {
            if (_isConnected) return;

            var clientInfo = (IPortableDeviceValues)new PortableDeviceValues();
            _device.Open(DeviceId, clientInfo);
            _isConnected = true;
        }

        public void Disconnect()
        {
            if (!_isConnected) return;

            _device.Close();
            _isConnected = false;
        }

        public PortableDeviceFolder GetContents()
        {
            var root = new PortableDeviceFolder("DEVICE", "DEVICE");
            var content = _device.Content();
            EnumerateContents(content, root);
            return root;
        }

        private static void EnumerateContents(IPortableDeviceContent content, PortableDeviceFolder parent)
        {
            var properties = content.Properties();
            var objectIds = content.EnumObjects(0, parent.Id, null);

            uint fetched = 0;
            var ids = new string[1];
            do
            {
                objectIds.Next(1, ids, out fetched);

                if (fetched <= 0) continue;

                var currentObject = WrapObject(properties, ids[0]);
                parent.Files.Add(currentObject);

                if (currentObject is PortableDeviceFolder folder)
                    EnumerateContents(content, folder);

            } while (fetched > 0);
        }

        public void DownloadFile(PortableDeviceFile file, string saveToPath)
        {
            var content = _device.Content();
            var resources = content.Transfer();

            var property = new PROPERTYKEY(
                new Guid(0xE81E79BE, 0x34F0, 0x41BF, 0xB5, 0x3F, 0xF1, 0xA0, 0x6A, 0xE8, 0x78, 0x42),
                0);

            uint optimalTransferSize = 0;
            var sourceStream = resources.GetStream(file.Id, in property, STGM.STGM_READ, out optimalTransferSize);

            var filename = Path.GetFileName(file.Name);
            using var targetStream = new FileStream(Path.Combine(saveToPath, filename), FileMode.Create, FileAccess.Write);

            unsafe
            {
                var buffer = new byte[1024];
                int bytesRead;
                do
                {
                    sourceStream.Read(buffer, 1024, new IntPtr(&bytesRead));
                    targetStream.Write(buffer, 0, bytesRead);
                } while (bytesRead > 0);
            }
        }

        public unsafe byte[] DownloadFileToStream(PortableDeviceFile file)
        {
            var content = _device.Content();
            var resources = content.Transfer();

            var property = new PROPERTYKEY(
                new Guid(0xE81E79BE, 0x34F0, 0x41BF, 0xB5, 0x3F, 0xF1, 0xA0, 0x6A, 0xE8, 0x78, 0x42),
                0);

            uint optimalTransferSize = 0;
            var stream = resources.GetStream(file.Id, in property, STGM.STGM_READ, out optimalTransferSize);

            using var memoryStream = new MemoryStream();
            int count;
            const int bufferSize = 8192;
            var buffer = new byte[bufferSize];
            do
            {
                stream.Read(buffer, bufferSize, new IntPtr(&count));
                memoryStream.Write(buffer, 0, count);
            } while (count >= bufferSize);

            Marshal.ReleaseComObject(stream);
            return memoryStream.ToArray();
        }

        public string DownloadFileToString(PortableDeviceFile file)
        {
            var content = _device.Content();
            var resources = content.Transfer();

            var property = new PROPERTYKEY(
                new Guid(0xE81E79BE, 0x34F0, 0x41BF, 0xB5, 0x3F, 0xF1, 0xA0, 0x6A, 0xE8, 0x78, 0x42),
                0);

            uint optimalTransferSize = 0;
            var sourceStream = resources.GetStream(file.Id, in property, STGM.STGM_READ, out optimalTransferSize);

            var targetStream = new MemoryStream();
            unsafe
            {
                var buffer = new byte[1024];
                int bytesRead;
                do
                {
                    sourceStream.Read(buffer, 1024, new IntPtr(&bytesRead));
                    targetStream.Write(buffer, 0, bytesRead);
                } while (bytesRead > 0);
            }

            targetStream.Position = 0;
            using var reader = new StreamReader(targetStream);
            return reader.ReadToEnd();
        }

        public void DeleteFile(PortableDeviceFile file)
        {
            var content = _device.Content();

            var objectIds = (IPortableDevicePropVariantCollection)new PortableDevicePropVariantCollection();
            var variant = new PROPVARIANT(file.Id, System.Runtime.InteropServices.VarEnum.VT_LPWSTR);
            objectIds.Add(variant);

            content.Delete(DELETE_OBJECT_OPTIONS.PORTABLE_DEVICE_DELETE_NO_RECURSION, objectIds);
        }

        private static PortableDeviceObject WrapObject(IPortableDeviceProperties properties, string objectId)
        {
            var keys = properties.GetSupportedProperties(objectId);
            var values = properties.GetValues(objectId, keys);

            var nameProp = new PROPERTYKEY(CreateFmtidGuid(), 4);
            var name = values.GetStringValue(in nameProp);

            var contentTypeProp = new PROPERTYKEY(CreateFmtidGuid(), 7);
            var contentType = values.GetGuidValue(in contentTypeProp);

            var folderType = new Guid(0x27E2E392, 0xA111, 0x48E0, 0xAB, 0x0C, 0xE1, 0x77, 0x05, 0xA0, 0x5F, 0x85);
            var functionalType = new Guid(0x99ED0160, 0x17FF, 0x4C44, 0x9D, 0x98, 0x1D, 0x7A, 0x6F, 0x94, 0x19, 0x21);

            if (contentType == folderType || contentType == functionalType)
                return new PortableDeviceFolder(objectId, name);

            var fileNameProp = new PROPERTYKEY(CreateFmtidGuid(), 12); // WPD_OBJECT_ORIGINAL_FILE_NAME
            var fileName = values.GetStringValue(in fileNameProp);
            return new PortableDeviceFile(objectId, fileName);
        }

        private IPortableDeviceValues GetRequiredPropertiesForContentType(string fileName, string parentObjectId)
        {
            var values = (IPortableDeviceValues)new PortableDeviceValues();

            var wpdObjectParentId = new PROPERTYKEY(CreateFmtidGuid(), 3);
            values.SetStringValue(in wpdObjectParentId, parentObjectId);

            var fileInfo = new FileInfo(fileName);
            var wpdObjectSize = new PROPERTYKEY(CreateFmtidGuid(), 11);
            values.SetUnsignedLargeIntegerValue(in wpdObjectSize, (ulong)fileInfo.Length);

            var wpdObjectOriginalFileName = new PROPERTYKEY(CreateFmtidGuid(), 12);
            values.SetStringValue(in wpdObjectOriginalFileName, Path.GetFileName(fileName));

            var wpdObjectName = new PROPERTYKEY(CreateFmtidGuid(), 4);
            values.SetStringValue(in wpdObjectName, Path.GetFileName(fileName));

            return values;
        }

        private IPortableDeviceValues GetRequiredPropertiesForContentTypeFromStream(MemoryStream inputStream, string fileName, string parentObjectId)
        {
            var values = (IPortableDeviceValues)new PortableDeviceValues();

            var wpdObjectParentId = new PROPERTYKEY(CreateFmtidGuid(), 3);
            values.SetStringValue(in wpdObjectParentId, parentObjectId);

            var wpdObjectSize = new PROPERTYKEY(CreateFmtidGuid(), 11);
            values.SetUnsignedLargeIntegerValue(in wpdObjectSize, (ulong)inputStream.Length);

            var wpdObjectOriginalFileName = new PROPERTYKEY(CreateFmtidGuid(), 12);
            values.SetStringValue(in wpdObjectOriginalFileName, fileName);

            var wpdObjectName = new PROPERTYKEY(CreateFmtidGuid(), 4);
            values.SetStringValue(in wpdObjectName, fileName);

            return values;
        }

        public void TransferContentToDevice(string fileName, string parentObjectId)
        {
            var content = _device.Content();
            var values = GetRequiredPropertiesForContentType(fileName, parentObjectId);

            uint optimalTransferSizeBytes = 0;
            IStream? tempStream = null;
            content.CreateObjectWithPropertiesAndData(values, out tempStream, ref optimalTransferSizeBytes);

            try
            {
                using var sourceStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                var buffer = new byte[optimalTransferSizeBytes];
                int bytesRead;
                do
                {
                    bytesRead = sourceStream.Read(buffer, 0, (int)optimalTransferSizeBytes);
                    tempStream!.Write(buffer, bytesRead, IntPtr.Zero);
                } while (bytesRead > 0);
                tempStream!.Commit(0);
            }
            finally
            {
                if (tempStream != null)
                    Marshal.ReleaseComObject(tempStream);
            }
        }

        public void TransferContentToDeviceFromStream(string fileName, MemoryStream inputStream, string parentObjectId)
        {
            var content = _device.Content();
            var values = GetRequiredPropertiesForContentTypeFromStream(inputStream, fileName, parentObjectId);

            uint optimalTransferSizeBytes = 0;
            IStream? tempStream = null;
            content.CreateObjectWithPropertiesAndData(values, out tempStream, ref optimalTransferSizeBytes);

            try
            {
                using var memoryStream = inputStream;
                var buffer = new byte[optimalTransferSizeBytes];
                int cb;
                do
                {
                    cb = memoryStream.Read(buffer, 0, (int)optimalTransferSizeBytes);
                    tempStream!.Write(buffer, cb, IntPtr.Zero);
                } while (cb > 0);
                tempStream!.Commit(0);
            }
            finally
            {
                if (tempStream != null)
                    Marshal.ReleaseComObject(tempStream);
            }
        }

        private static Guid CreateFmtidGuid() =>
            new Guid(0xEF6B490D, 0x5CD8, 0x437A, 0xAF, 0xFC, 0xDA, 0x8B, 0x60, 0xEE, 0x4A, 0x3C);
    }
}
