using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PortableDevices;

namespace ConsoleTest
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
            var imgPath = "C:\\Users\\Lasha\\Desktop\\GettyImages-87948663.jpg";
            var image = Image.FromFile(imgPath);
            byte[] imageB;
            using (var ms = new MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
                imageB = ms.ToArray();
            }
            //Tablet.TransferContentToDevice("C:\\Users\\Lasha\\Desktop\\GettyImages-87948663.jpg", nacresFolder.Id);
            Tablet.TransferContentToDeviceFromStream(Path.GetFileName(imgPath), new MemoryStream(imageB), nacresFolder.Id);
            var filledQuestionnairesFile = nacresFolder.Files.FirstOrDefault(x => x.Name == "FilledQuestionnaires.txt" || x.Name == "FilledQuestionnaires") as PortableDeviceFile;
            //var filledQuestionnairesTempFile = Path.Combine(Path.GetTempPath(), Constants.FILLED_QUESTIONNAIRES_JSON);
            var asdf = Tablet.DownloadFileToString(filledQuestionnairesFile);
            Tablet.DownloadFile(filledQuestionnairesFile, "C:\\");



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
                DisplayFolderContents((PortableDeviceFolder)portableDeviceObject);
            }
        }

        public static void DisplayFolderContents(PortableDeviceFolder folder)
        {
            foreach (var item in folder.Files)
            {
                Console.WriteLine(item.Name);

                if (item is PortableDeviceFolder)
                {
                    DisplayFolderContents((PortableDeviceFolder)item);
                }
            }
        }
    }
}
