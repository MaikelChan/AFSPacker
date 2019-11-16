using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace AFSPacker
{
    class AFS
    {
        const uint HEADER_MAGIC_1 = 0x00534641; // AFS
        const uint HEADER_MAGIC_2 = 0x20534641;
        const string NULL_FILE = "#NULL#";

        public void CreateAFS(string inputDirectory, string outputFile, string filesList = null, bool preserveFileNames = true)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Packaging files...\n\n");

            string[] inputFiles;

            if (string.IsNullOrEmpty(filesList))
            {
                inputFiles = Directory.GetFiles(inputDirectory);
            }
            else
            {
                inputFiles = File.ReadAllLines(filesList);
            }

            using (FileStream fs1 = new FileStream(outputFile, FileMode.Create))
            using (BinaryWriter bw = new BinaryWriter(fs1))
            {
                bw.Write(HEADER_MAGIC_1);
                bw.Write((uint)inputFiles.Length);

                // Generate TOC and FileNameDirectory

                TableOfContents[] toc = new TableOfContents[inputFiles.Length];
                FileAttributes[] attributes = new FileAttributes[inputFiles.Length];

                uint currentOffset = Pad((uint)(8 + (8 * inputFiles.Length) + 8), 0x800);  // Header + TOC + AttributeTable Offset and size

                for (int n = 0; n < inputFiles.Length; n++)
                {
                    if (inputFiles[n] == NULL_FILE)
                    {
                        toc[n].FileSize = 0;
                        toc[n].Offset = 0;

                        if (preserveFileNames)
                        {
                            //FileNameDirectory
                            attributes[n].FileName = string.Empty;
                            attributes[n].Year = 0;
                            attributes[n].Month = 0;
                            attributes[n].Day = 0;
                            attributes[n].Hour = 0;
                            attributes[n].Minute = 0;
                            attributes[n].Second = 0;
                            attributes[n].FileSize = 0;
                        }
                    }
                    else
                    {
                        FileInfo f = new FileInfo(inputFiles[n]);

                        //TOC
                        toc[n].FileSize = (uint)f.Length;
                        toc[n].Offset = currentOffset;

                        currentOffset += toc[n].FileSize;
                        currentOffset = Pad(currentOffset, 0x800);

                        if (preserveFileNames)
                        {
                            //FileNameDirectory
                            attributes[n].FileName = Path.GetFileName(inputFiles[n]);
                            attributes[n].Year = (ushort)f.LastWriteTime.Year;
                            attributes[n].Month = (ushort)f.LastWriteTime.Month;
                            attributes[n].Day = (ushort)f.LastWriteTime.Day;
                            attributes[n].Hour = (ushort)f.LastWriteTime.Hour;
                            attributes[n].Minute = (ushort)f.LastWriteTime.Minute;
                            attributes[n].Second = (ushort)f.LastWriteTime.Second;
                            attributes[n].FileSize = (uint)f.Length;
                        }
                    }

                    Console.CursorLeft = 0;
                    if (preserveFileNames) Console.Write($"Processing TOC and attributes... {n}/{inputFiles.Length - 1}");
                    else Console.Write($"Processing TOC... {n}/{inputFiles.Length - 1}");
                }

                Console.WriteLine();

                //Write TOC to file
                for (int n = 0; n < inputFiles.Length; n++)
                {
                    bw.Write(toc[n].Offset);
                    bw.Write(toc[n].FileSize);

                    Console.CursorLeft = 0;
                    Console.Write($"Writing TOC... {n}/{inputFiles.Length - 1}");
                }

                Console.WriteLine();

                uint attributeTableOffset = 0;
                uint attributeTableSize = 0;

                if (preserveFileNames)
                {
                    //Write Filename Directory Offset and Size
                    attributeTableOffset = currentOffset;
                    attributeTableSize = (uint)(inputFiles.Length * 0x30);
                    fs1.Seek(toc[0].Offset - 8, SeekOrigin.Begin);
                    bw.Write(attributeTableOffset);
                    bw.Write(attributeTableSize);
                }

                //Write files data to file
                for (int n = 0; n < inputFiles.Length; n++)
                {
                    if (inputFiles[n] != NULL_FILE)
                    {
                        byte[] data = File.ReadAllBytes(inputFiles[n]);
                        fs1.Seek(toc[n].Offset, SeekOrigin.Begin);
                        fs1.Write(data, 0, data.Length);
                    }

                    Console.CursorLeft = 0;
                    Console.Write($"Writing files... {n}/{inputFiles.Length - 1}");
                }

                Console.WriteLine();

                if (preserveFileNames)
                {
                    //Write Filename Directory
                    fs1.Seek(attributeTableOffset, SeekOrigin.Begin);
                    for (int n = 0; n < inputFiles.Length; n++)
                    {
                        byte[] name = Encoding.Default.GetBytes(attributes[n].FileName);
                        fs1.Write(name, 0, name.Length);
                        fs1.Seek(0x20 - name.Length, SeekOrigin.Current);

                        bw.Write(attributes[n].Year);
                        bw.Write(attributes[n].Month);
                        bw.Write(attributes[n].Day);
                        bw.Write(attributes[n].Hour);
                        bw.Write(attributes[n].Minute);
                        bw.Write(attributes[n].Second);
                        bw.Write(attributes[n].FileSize);

                        Console.CursorLeft = 0;
                        Console.Write($"Writing attributes... {n}/{inputFiles.Length - 1}");
                    }
                    Console.WriteLine();
                }

                //Pad final 0s
                long currentPosition = fs1.Position;
                long eof = Pad((uint)fs1.Position, 0x800);
                for (long n = currentPosition; n < eof; n++) bw.Write((byte)0);
            }
        }

        public void ExtractAFS(string inputFile, string outputDirectory, string filesList = null)
        {
            bool areThereAttributes = true;

            using (FileStream fs1 = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs1))
            {
                uint magic = br.ReadUInt32();
                if (magic != HEADER_MAGIC_1 && magic != HEADER_MAGIC_2) //If Magic is different than AFS
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Input file doesn't seem to be a valid AFS file.");
                    return;
                }

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Extracting files...\n\n");

                uint numberOfFiles = br.ReadUInt32();

                TableOfContents[] toc = new TableOfContents[numberOfFiles];
                FileAttributes[] atrributes = new FileAttributes[numberOfFiles];

                //Read TOC
                for (int n = 0; n < numberOfFiles; n++)
                {
                    toc[n].Offset = br.ReadUInt32();
                    toc[n].FileSize = br.ReadUInt32();

                    Console.CursorLeft = 0;
                    Console.Write($"Reading TOC... {n}/{numberOfFiles - 1}");
                }

                Console.WriteLine();

                //Read Filename Directory Offset and Size
                uint attributeTableOffset = 0;
                uint attributeTableSize = 0;
                while (fs1.Position < toc[0].Offset && attributeTableOffset == 0)
                {
                    //fs1.Seek(TOC[0].Offset - 8, SeekOrigin.Begin);
                    attributeTableOffset = br.ReadUInt32();
                    attributeTableSize = br.ReadUInt32();
                }

                if (attributeTableOffset == 0) areThereAttributes = false;

                string[] fileName = new string[numberOfFiles];

                if (areThereAttributes)
                {
                    //Read Attribute table
                    fs1.Seek(attributeTableOffset, SeekOrigin.Begin);

                    for (int n = 0; n < numberOfFiles; n++)
                    {
                        byte[] name = new byte[32];
                        fs1.Read(name, 0, name.Length);
                        fileName[n] = Encoding.Default.GetString(name).Replace("\0", "");

                        atrributes[n].Year = br.ReadUInt16();
                        atrributes[n].Month = br.ReadUInt16();
                        atrributes[n].Day = br.ReadUInt16();
                        atrributes[n].Hour = br.ReadUInt16();
                        atrributes[n].Minute = br.ReadUInt16();
                        atrributes[n].Second = br.ReadUInt16();
                        atrributes[n].FileSize = br.ReadUInt32();

                        Console.CursorLeft = 0;
                        Console.Write($"Reading attributes table... {n}/{numberOfFiles - 1}");
                    }

                    fileName = CheckForDuplicatedFilenames(fileName);
                }
                else
                {
                    for (int n = 0; n < numberOfFiles; n++)
                    {
                        fileName[n] = n.ToString("00000000", CultureInfo.InvariantCulture);
                    }
                }

                Console.WriteLine();
                Console.WriteLine();

                //Extract files
                if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);

                string[] filelist = new string[numberOfFiles];

                for (int n = 0; n < numberOfFiles; n++)
                {
                    if (toc[n].FileSize == 0 && toc[n].Offset == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"Warning: File \"{n}\" is a null file; Skipping.\n");
                        Console.ForegroundColor = ConsoleColor.White;

                        filelist[n] = NULL_FILE;

                        continue;
                    }

                    Console.CursorLeft = 0;
                    Console.WriteLine($"\nReading files... {n}/{numberOfFiles - 1}");

                    byte[] filedata = new byte[toc[n].FileSize];
                    fs1.Seek(toc[n].Offset, SeekOrigin.Begin);
                    fs1.Read(filedata, 0, filedata.Length);

                    string outputFile = Path.Combine(outputDirectory, fileName[n]);
                    if (File.Exists(outputFile))
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"Warning: File \"{outputFile}\" already exists. Overwriting.");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    File.WriteAllBytes(outputFile, filedata);

                    if (areThereAttributes)
                    {
                        try
                        {
                            DateTime date = new DateTime(atrributes[n].Year, atrributes[n].Month, atrributes[n].Day, atrributes[n].Hour, atrributes[n].Minute, atrributes[n].Second);
                            File.SetLastWriteTime(outputFile, date);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.WriteLine("Warning: Invalid date. Ignoring.");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                    }

                    filelist[n] = outputFile; //Save the list of files in order to have the original order
                }

                Console.WriteLine();

                if (!string.IsNullOrEmpty(filesList)) File.WriteAllLines(filesList, filelist);
            }
        }

        uint Pad(uint value, uint padBytes)
        {
            if ((value % padBytes) != 0) return value + (padBytes - (value % padBytes));
            else return value;
        }

        string[] CheckForDuplicatedFilenames(string[] fileNames)
        {
            string[] output = new string[fileNames.Length];

            for (int n = 0; n < fileNames.Length; n++)
            {
                int count = 0;

                for (int o = 0; o < n; o++)
                {
                    if (fileNames[n] == fileNames[o]) count++;
                }

                if (count == 0) output[n] = fileNames[n];
                else output[n] = $"{Path.GetFileNameWithoutExtension(fileNames[n])} ({count}){Path.GetExtension(fileNames[n])}";
            }

            return output;
        }

        public struct TableOfContents
        {
            public uint Offset;
            public uint FileSize;
        }

        public struct FileAttributes
        {
            public string FileName;
            public ushort Year;
            public ushort Month;
            public ushort Day;
            public ushort Hour;
            public ushort Minute;
            public ushort Second;
            public uint FileSize;
        }
    }
}