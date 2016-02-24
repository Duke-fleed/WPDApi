using System;
using System.Linq;

namespace PortableDevices
{
    class Program
    {
        public static PortableDevice Tablet;
        public static string NacresFolderId;
        static void Main()
        {
            var devices = new PortableDeviceCollection();
            devices.Refresh();

            Tablet = devices.First();
            Tablet.Connect();

            var root = Tablet.GetContents();
            foreach (var resource in root.Files)
            {
                DisplayResourceContents(resource);
            }

            var nacresFolder = (root.Files.FirstOrDefault() as PortableDeviceFolder).Files.FirstOrDefault(x => x.Name == "Nacres") as PortableDeviceFolder;
            var testFile = nacresFolder.Files.FirstOrDefault(x => x.Name == "TestFile") as PortableDeviceFile;
            Tablet.DeleteFile(testFile);

            Tablet.TransferContentToDevice("C:\\Users\\lasm\\Desktop\\TestFile.txt", NacresFolderId);
            Tablet.Disconnect();

            Console.WriteLine();
            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }

        public static void DisplayResourceContents(PortableDeviceObject portableDeviceObject)
        {
            Console.WriteLine(portableDeviceObject.Name);
            if (portableDeviceObject is PortableDeviceFolder)
            {
                DisplayFolderContents((PortableDeviceFolder) portableDeviceObject);
            }
        }

        public static void DisplayFolderContents(PortableDeviceFolder folder)
        {
            foreach (var item in folder.Files)
            {
                Console.WriteLine(item.Name);

                if (item is PortableDeviceFolder)
                {
                    DisplayFolderContents((PortableDeviceFolder) item);
                }
            }
        }
    }
}
