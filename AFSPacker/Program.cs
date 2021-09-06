using AFSLib;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;

namespace AFSPacker
{
    class Program
    {
        static void Main(string[] args)
        {
            ShowHeader();

            if (args.Length < 2)
            {
                ShowUsage();
                return;
            }

#if !DEBUG
            try
            {
#endif
                if (args[0] == "-c")
                {
                    if (args.Length != 3)
                    {
                        ShowUsage();
                        return;
                    }

                    AFSMetadata metadata = AFSMetadata.LoadFromFile(args[1] + ".json");

                    using (AFS afs = new AFS())
                    {
                        afs.NotifyProgress += Progress;

                        afs.HeaderMagicType = metadata.HeaderMagicType;
                        afs.AttributesInfoType = metadata.AttributesInfoType;
                        afs.EntryBlockAlignment = metadata.EntryBlockAlignment;

                        for (int e = 0; e < metadata.Entries.Length; e++)
                        {
                            if (metadata.Entries[e].IsNull)
                            {
                                afs.AddNullEntry();
                            }
                            else
                            {
                                string filePath = Path.Combine(args[1], metadata.Entries[e].FileName);
                                afs.AddEntryFromFile(filePath, metadata.Entries[e].Name);
                            }
                        }

                        afs.SaveToFile(args[2]);
                    }
                }
                else if (args[0] == "-e")
                {
                    if (args.Length != 3)
                    {
                        ShowUsage();
                        return;
                    }

                    using (AFS afs = new AFS(args[1]))
                    {
                        afs.NotifyProgress += Progress;

                        afs.ExtractAllEntriesToDirectory(args[2]);

                        AFSMetadata metadata = new AFSMetadata(afs);
                        metadata.SaveToFile(args[2] + ".json");
                    }
                }
                else if (args[0] == "-i")
                {
                    if (args.Length != 2)
                    {
                        ShowUsage();
                        return;
                    }

                    using (AFS afs = new AFS(args[1]))
                    {
                        ReadOnlyCollection<Entry> entries = afs.Entries;

                        Console.ForegroundColor = ConsoleColor.White;

                        Console.WriteLine();
                        Console.WriteLine($"File name             : {Path.GetFileName(args[1])}");
                        Console.WriteLine($"Header magic          : {afs.HeaderMagicType}");
                        Console.WriteLine($"Attributes info type  : {afs.AttributesInfoType}");
                        Console.WriteLine($"Entry Block Alignment : {afs.EntryBlockAlignment}");
                        Console.WriteLine($"Number of entries     : {afs.EntryCount}");

                        Console.WriteLine();
                        Console.WriteLine(" Index    | Name                             | Size       | Last Write Time");
                        Console.WriteLine(" ---------------------------------------------------------------------------------");

                        for (int e = 0; e < afs.EntryCount; e++)
                        {
                            Console.ForegroundColor = (e & 1) == 0 ? ConsoleColor.Gray : ConsoleColor.White;

                            string index = e.ToString("00000000");

                            if (entries[e] is NullEntry)
                            {
                                string name = "(null)".PadRight(32);
                                string size = "N/A".PadRight(10);
                                string time = "N/A";

                                Console.WriteLine($" {index} | {name} | {size} | {time}");
                            }
                            else
                            {
                                DataEntry dataEntry = entries[e] as DataEntry;

                                string name = afs.ContainsAttributes ? dataEntry.Name.PadRight(32) : "N/A".PadRight(32);
                                string size = dataEntry.Size.ToString("X8");
                                string time = afs.ContainsAttributes ? dataEntry.LastWriteTime.ToString() : "N/A";

                                Console.WriteLine($" {index} | {name} | 0x{size} | {time}");
                            }
                        }
                    }
                }
#if !DEBUG
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] [Error] {e.Message}");
            }
#endif

            Console.ForegroundColor = ConsoleColor.Gray;
        }

        static void Progress(NotificationType type, string message)
        {
            switch (type)
            {
                default:
                case NotificationType.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case NotificationType.Warning:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;
                case NotificationType.Success:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
            }

            Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] [{type}] {message}");
        }

        static void ShowHeader()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            string v = $"{version.Major}.{version.Minor}.{version.Build}";

            Version libVersion = AFS.GetVersion();
            string lv = $"{libVersion.Major}.{libVersion.Minor}.{libVersion.Build}";

            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine();
            Console.WriteLine("        #----------------------------------------------------------------#");

            Console.WriteLine("        #                   AFS Packer - Version " + v + "                   #");
            Console.Write("        #             ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("https://github.com/MaikelChan/AFSPacker");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("            #");

            Console.WriteLine("        #                                                                #");

            Console.WriteLine("        #                Powered by AFSLib - Version " + lv + "               #");
            Console.Write("        #              ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("https://github.com/MaikelChan/AFSLib");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("              #");

            Console.WriteLine("        #                                                                #");
            Console.WriteLine("        #                    By MaikelChan / PacoChan                    #");
            Console.WriteLine("        #----------------------------------------------------------------#\n\n");
        }

        static void ShowUsage()
        {
            Console.ForegroundColor = ConsoleColor.Gray;

            Console.WriteLine("Usage:\n");

            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine("  AFSPacker -e <input_afs_file> <output_dir>  :  Extract AFS archive");
            Console.WriteLine("  AFSPacker -c <input_dir> <output_afs_file>  :  Create AFS archive");
            Console.WriteLine("  AFSPacker -i <input_afs_file>               :  Show AFS information\n");
        }
    }
}