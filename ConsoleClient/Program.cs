using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using PortableDevices;

namespace ConsoleClient
{
    [SupportedOSPlatform("windows")]
    class Program
    {
        public static PortableDevice? Tablet;

        static void Main()
        {
            //Connect to MTP devices and pick up the first one
            var devices = new PortableDeviceCollection();
            devices.Refresh();

            Tablet = devices.First();
            Tablet.Connect();

            //Getting root directory
            var root = Tablet.GetContents();

            //Displaying directory content in console
            foreach (var resource in root.Files)
            {
                DisplayResourceContents(resource);
            }

            //Finding necessary folder inside the root
            var folder = (root.Files.FirstOrDefault() as PortableDeviceFolder)?
                .Files.FirstOrDefault(x => x.Name == "Folder") as PortableDeviceFolder;

            //Finding file inside the folder
            var file = folder?.Files?.FirstOrDefault(x => x.Name == "File");

            //Transferring file into byte array
            var fileIntoByteArr = Tablet.DownloadFileToStream((file as PortableDeviceFile)!);

            //Transferring file into file system
            Tablet.DownloadFile((file as PortableDeviceFile)!, "\\LOCALPATH");

            //Transferring file from file system into device folder
            Tablet.TransferContentToDevice("\\LOCALPATH", folder!.Id);

            //Transferring file from stream into device folder
            var imgPath = "\\LOCALPATH";
            var image = Image.FromFile(imgPath);
            byte[] imageB;
            using (var ms = new MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
                imageB = ms.ToArray();
            }
            Tablet.TransferContentToDeviceFromStream("FILE NAME", new MemoryStream(imageB), folder.Id);

            //Close the connection
            Tablet.Disconnect();

            Console.WriteLine();
            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }

        public static void DisplayResourceContents(PortableDeviceObject portableDeviceObject)
        {
            Console.WriteLine(portableDeviceObject.Name);
            if (portableDeviceObject is PortableDeviceFolder folder)
            {
                DisplayFolderContents(folder);
            }
        }

        public static void DisplayFolderContents(PortableDeviceFolder folder)
        {
            foreach (var item in folder.Files)
            {
                Console.WriteLine(item.Name);

                if (item is PortableDeviceFolder subFolder)
                {
                    DisplayFolderContents(subFolder);
                }
            }
        }
    }
}
