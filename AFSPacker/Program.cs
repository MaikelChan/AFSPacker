using AFSLib;
using System;
using System.Reflection;

namespace AFSPacker
{
    class Program
    {
        static void Main(string[] args)
        {
            ShowHeader();

            if (args.Length != 3)
            {
                ShowUsage();
                return;
            }

            AFS.NotifyProgress += Progress;

            try
            {
                if (args[0] == "-c")
                {
                    AFS.CreateAFS(args[1], args[2]);
                }
                else if (args[0] == "-e")
                {
                    AFS.ExtractAFS(args[1], args[2]);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Progress(AFS.NotificationTypes.Error, e.Message);
            }

            AFS.NotifyProgress -= Progress;

            Console.ForegroundColor = ConsoleColor.Gray;
        }

        static void Progress(AFS.NotificationTypes type, string message)
        {
            switch (type)
            {
                default:
                case AFS.NotificationTypes.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case AFS.NotificationTypes.Warning:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;
                case AFS.NotificationTypes.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case AFS.NotificationTypes.Success:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
            }

            Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] [{type}] {message}");
        }

        static void ShowHeader()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            string v = $"{version.Major}.{version.Minor}.{version.Build}";

            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine();
            Console.WriteLine("        #----------------------------------------------------------------#");
            Console.WriteLine("        #                   AFS Packer - Version " + v + "                   #");
            Console.Write("        #      By PacoChan - ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("https://github.com/MaikelChan/AFSPacker");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("     #");
            Console.WriteLine("        #----------------------------------------------------------------#\n\n");
        }

        static void ShowUsage()
        {
            Console.ForegroundColor = ConsoleColor.Gray;

            Console.WriteLine("Usage:\n");

            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine("  AFSPacker -e <input_afs_file> <output_dir>  :  Extract AFS archive");
            Console.WriteLine("  AFSPacker -c <input_dir> <output_afs_file>  :  Create AFS archive\n");
        }
    }
}