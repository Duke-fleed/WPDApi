using System;
using System.IO;
using PortableDeviceApiLib;
using PortableDeviceTypesLib;
using _tagpropertykey = PortableDeviceApiLib._tagpropertykey;
using IPortableDeviceKeyCollection = PortableDeviceApiLib.IPortableDeviceKeyCollection;
using IPortableDeviceValues = PortableDeviceApiLib.IPortableDeviceValues;
using System.Runtime.InteropServices;

namespace PortableDevices
{
    public class PortableDevice
    {
        private bool _isConnected;

        public PortableDevice(string deviceId)
        {
            PortableDeviceClass = new PortableDeviceClass();
            DeviceId = deviceId;
        }

        internal PortableDeviceClass PortableDeviceClass { get; }

        public string DeviceId { get; set; }

        public string FriendlyName
        {
            get
            {
                if (!_isConnected)
                {
                    throw new InvalidOperationException("Not connected to device.");
                }

                // Retrieve the properties of the device
                IPortableDeviceContent content;
                IPortableDeviceProperties properties;
                PortableDeviceClass.Content(out content);
                content.Properties(out properties);

                // Retrieve the values for the properties
                IPortableDeviceValues propertyValues;
                properties.GetValues("DEVICE", null, out propertyValues);

                // Identify the property to retrieve
                var property = new _tagpropertykey
                {
                    fmtid = new Guid(0x26D4979A, 0xE643, 0x4626, 0x9E, 0x2B,
                        0x73, 0x6D, 0xC0, 0xC9, 0x2F, 0xDC),
                    pid = 12
                };

                // Retrieve the friendly name
                string propertyValue;
                propertyValues.GetStringValue(ref property, out propertyValue);

                return propertyValue;
            }
        }

        public void Connect()
        {
            if (_isConnected) return;

            var clientInfo = (IPortableDeviceValues) new PortableDeviceValuesClass();
            PortableDeviceClass.Open(DeviceId, clientInfo);
            _isConnected = true;
        }

        public void Disconnect()
        {
            if (!_isConnected) return; 

            PortableDeviceClass.Close();
            _isConnected = false;
        }

        public PortableDeviceFolder GetContents()
        {
            var root = new PortableDeviceFolder("DEVICE", "DEVICE");

            IPortableDeviceContent content;
            PortableDeviceClass.Content(out content);

            EnumerateContents(ref content, root);

            return root;
        }

        private static void EnumerateContents(ref IPortableDeviceContent content,
            PortableDeviceFolder parent)
        {
            // Get the properties of the object
            IPortableDeviceProperties properties;
            content.Properties(out properties);

            // Enumerate the items contained by the current object
            IEnumPortableDeviceObjectIDs objectIds;
            content.EnumObjects(0, parent.Id, null, out objectIds);

            uint fetched = 0;
            do
            {
                string objectId;

                objectIds.Next(1, out objectId, ref fetched);

                if (fetched <= 0) continue;

                var currentObject = WrapObject(properties, objectId);

                parent.Files.Add(currentObject);

                if (currentObject is PortableDeviceFolder)
                {
                    EnumerateContents(ref content, (PortableDeviceFolder)currentObject);
                }
            } while (fetched > 0);
        }

        public void DownloadFile(PortableDeviceFile file, string saveToPath)
        {
            IPortableDeviceContent content;
            PortableDeviceClass.Content(out content);

            IPortableDeviceResources resources;
            content.Transfer(out resources);

            PortableDeviceApiLib.IStream wpdStream;
            uint optimalTransferSize = 0;

            var property = new _tagpropertykey
            {
                fmtid = new Guid(0xE81E79BE, 0x34F0, 0x41BF, 0xB5, 0x3F, 0xF1, 0xA0, 0x6A, 0xE8, 0x78, 0x42),
                pid = 0
            };

            resources.GetStream(file.Id, ref property, 0, ref optimalTransferSize, out wpdStream);

            var sourceStream = (System.Runtime.InteropServices.ComTypes.IStream)wpdStream;

            var filename = Path.GetFileName(file.Name);
            var targetStream = new FileStream(Path.Combine(saveToPath, filename), FileMode.Create, FileAccess.Write);

            unsafe
            {
                var buffer = new byte[1024];
                int bytesRead;
                do
                {
                    sourceStream.Read(buffer, 1024, new IntPtr(&bytesRead));
                    targetStream.Write(buffer, 0, 1024);
                } while (bytesRead > 0);
                targetStream.Close();
            }
        }

        public unsafe byte[] DownloadFileToStream(PortableDeviceFile file)
        {
            IPortableDeviceContent iportableDeviceContent;
            PortableDeviceClass.Content(out iportableDeviceContent);
            IPortableDeviceResources iportableDeviceResources;
            iportableDeviceContent.Transfer(out iportableDeviceResources);
            uint num = 0;
            _tagpropertykey tagpropertykey;
            tagpropertykey.fmtid = new Guid(3894311358U, 13552, 16831, 181, 63, 241, 160, 106, 232, 120, 66);
            tagpropertykey.pid = 0;
            PortableDeviceApiLib.IStream istream;
            iportableDeviceResources.GetStream(file.Id, ref tagpropertykey, 0U, ref num, out istream);
            var stream = (System.Runtime.InteropServices.ComTypes.IStream)istream;
            using (var memoryStream = new MemoryStream())
            {
                var count = 0;
                var cb = 8192;
                var numArray = new byte[cb];
                do
                {
                    stream.Read(numArray, cb, new IntPtr(&count));
                    memoryStream.Write(numArray, 0, count);
                }
                while (count >= cb);
                Marshal.ReleaseComObject(stream);
                Marshal.ReleaseComObject(istream);
                return memoryStream.ToArray();
            }
        }

        public string DownloadFileToString(PortableDeviceFile file)
        {
            IPortableDeviceContent content;
            PortableDeviceClass.Content(out content);

            IPortableDeviceResources resources;
            content.Transfer(out resources);

            PortableDeviceApiLib.IStream wpdStream;
            uint optimalTransferSize = 0;

            var property = new _tagpropertykey
            {
                fmtid = new Guid(0xE81E79BE, 0x34F0, 0x41BF, 0xB5, 0x3F, 0xF1, 0xA0, 0x6A, 0xE8, 0x78, 0x42),
                pid = 0
            };

            resources.GetStream(file.Id, ref property, 0, ref optimalTransferSize, out wpdStream);

            var sourceStream = (System.Runtime.InteropServices.ComTypes.IStream)wpdStream;
            var targetStream = new MemoryStream();
            unsafe
            {
                var buffer = new byte[1024];
                int bytesRead;
                do
                {
                    sourceStream.Read(buffer, 1024, new IntPtr(&bytesRead));
                    targetStream.Write(buffer, 0, 1024);
                } while (bytesRead > 0);
                //targetStream.Close();
            }

            var reader = new StreamReader(targetStream);
            var text =  reader.ReadToEnd();
            targetStream.Close();

            return text;
        }

        private static void StringToPropVariant(
            string value,
            out PortableDeviceApiLib.tag_inner_PROPVARIANT propvarValue)
        {
            var pValues = (IPortableDeviceValues) new PortableDeviceValuesClass();

            var wpdObjectId = new _tagpropertykey
            {
                fmtid = CreateFmtidGuid(),
                pid = 2
            };

            pValues.SetStringValue(ref wpdObjectId, value);

            pValues.GetValue(ref wpdObjectId, out propvarValue);
        }

        public void DeleteFile(PortableDeviceFile file)
        {
            IPortableDeviceContent content;
            PortableDeviceClass.Content(out content);

            var variant = new PortableDeviceApiLib.tag_inner_PROPVARIANT();
            StringToPropVariant(file.Id, out variant);

            var objectIds =
                new PortableDevicePropVariantCollection()
                as PortableDeviceApiLib.IPortableDevicePropVariantCollection;
            objectIds.Add(variant);

            content.Delete(0, objectIds, null);
        }

        private static PortableDeviceObject WrapObject(IPortableDeviceProperties properties,
            string objectId)
        {
            IPortableDeviceKeyCollection keys;
            properties.GetSupportedProperties(objectId, out keys);

            IPortableDeviceValues values;
            properties.GetValues(objectId, keys, out values);

            // Get the name of the object
            string name;
            var property = new _tagpropertykey
            {
                fmtid = CreateFmtidGuid(),
                pid = 4
            };
            values.GetStringValue(property, out name);

            // Get the type of the object
            Guid contentType;
            property = new _tagpropertykey
            {
                fmtid = CreateFmtidGuid(),
                pid = 7
            };
            values.GetGuidValue(property, out contentType);

            var folderType = new Guid(0x27E2E392, 0xA111, 0x48E0, 0xAB, 0x0C,
                                      0xE1, 0x77, 0x05, 0xA0, 0x5F, 0x85);
            var functionalType = new Guid(0x99ED0160, 0x17FF, 0x4C44, 0x9D, 0x98,
                                          0x1D, 0x7A, 0x6F, 0x94, 0x19, 0x21);

            if (contentType == folderType || contentType == functionalType)
            {
                return new PortableDeviceFolder(objectId, name);
            }
            property.pid = 12;//WPD_OBJECT_ORIGINAL_FILE_NAME
            values.GetStringValue(property, out name);
            return new PortableDeviceFile(objectId, name);
        }

        private IPortableDeviceValues GetRequiredPropertiesForContentType(string fileName, string parentObjectId)
        {
            var values = new PortableDeviceValues() as IPortableDeviceValues;

            var wpdObjectParentId = new _tagpropertykey
            {
                fmtid = CreateFmtidGuid(),
                pid = 3
            };
            values.SetStringValue(ref wpdObjectParentId, parentObjectId);

            var fileInfo = new FileInfo(fileName);

            var wpdObjectSize = new _tagpropertykey
            {
                fmtid = CreateFmtidGuid(),
                pid = 11
            };
            values.SetUnsignedLargeIntegerValue(wpdObjectSize, (ulong)fileInfo.Length);

            var wpdObjectOriginalFileName = new _tagpropertykey
            {
                fmtid = CreateFmtidGuid(),
                pid = 12
            };
            values.SetStringValue(wpdObjectOriginalFileName, Path.GetFileName(fileName));

            var wpdObjectName = new _tagpropertykey
            {
                fmtid = CreateFmtidGuid(),
                pid = 4
            };
            values.SetStringValue(wpdObjectName, Path.GetFileName(fileName));

            return values;
        }

        private IPortableDeviceValues GetRequiredPropertiesForContentTypeFromStream(MemoryStream inputStream, string fileName, string parentObjectId)
        {
            var values = new PortableDeviceValues() as IPortableDeviceValues;

            var wpdObjectParentId = new _tagpropertykey
            {
                fmtid = CreateFmtidGuid(),
                pid = 3
            };
            values.SetStringValue(ref wpdObjectParentId, parentObjectId);

            var wpdObjectSize = new _tagpropertykey
            {
                fmtid = CreateFmtidGuid(),
                pid = 11
            };
            values.SetUnsignedLargeIntegerValue(wpdObjectSize, (ulong)inputStream.Length);

            var wpdObjectOriginalFileName = new _tagpropertykey
            {
                fmtid = CreateFmtidGuid(),
                pid = 12
            };
            values.SetStringValue(wpdObjectOriginalFileName, fileName);

            var wpdObjectName = new _tagpropertykey
            {
                fmtid = CreateFmtidGuid(),
                pid = 4
            };

            values.SetStringValue(wpdObjectName, fileName);

            return values;
        }

        public void TransferContentToDevice(string fileName, string parentObjectId)
        {
            IPortableDeviceContent content;
            PortableDeviceClass.Content(out content);


            var values =
                GetRequiredPropertiesForContentType(fileName, parentObjectId);


            PortableDeviceApiLib.IStream tempStream;
            uint optimalTransferSizeBytes = 0;
            content.CreateObjectWithPropertiesAndData(
                values,
                out tempStream,
                ref optimalTransferSizeBytes,
                null);

            var targetStream =
                (System.Runtime.InteropServices.ComTypes.IStream)tempStream;
            

            try
            {
                using (var sourceStream =
                    new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    var buffer = new byte[optimalTransferSizeBytes];
                    int bytesRead;
                    do
                    {
                        bytesRead = sourceStream.Read(
                            buffer, 0, (int)optimalTransferSizeBytes);
                        var pcbWritten = IntPtr.Zero;
                        targetStream.Write(
                            buffer, bytesRead, pcbWritten);
                    } while (bytesRead > 0);
                }
                targetStream.Commit(0);
            }
            finally
            {
                Marshal.ReleaseComObject(tempStream);
            }

        }

        public void TransferContentToDeviceFromStream(string fileName, MemoryStream inputStream, string parentObjectId)
        {
            IPortableDeviceContent content;
            PortableDeviceClass.Content(out content);


            var values =
                GetRequiredPropertiesForContentTypeFromStream(inputStream, fileName, parentObjectId);


            PortableDeviceApiLib.IStream tempStream;
            uint optimalTransferSizeBytes = 0;
            content.CreateObjectWithPropertiesAndData(
                values,
                out tempStream,
                ref optimalTransferSizeBytes,
                null);

            var targetStream =
                (System.Runtime.InteropServices.ComTypes.IStream)tempStream;
            try
            {
                using (var memoryStream = inputStream)
                {
                    var numArray = new byte[(int)optimalTransferSizeBytes];
                    int cb;
                    do
                    {
                        cb = memoryStream.Read(numArray, 0, (int)optimalTransferSizeBytes);
                        var zero = IntPtr.Zero;
                        targetStream.Write(numArray, cb, zero);
                    }
                    while (cb > 0);
                }
                targetStream.Commit(0);
            }
            finally
            {
                Marshal.ReleaseComObject(tempStream);
            }
        }

        private static Guid CreateFmtidGuid() => new Guid(0xEF6B490D, 0x5CD8, 0x437A, 0xAF, 0xFC, 0xDA, 0x8B, 0x60, 0xEE, 0x4A,
            0x3C);
    }
}