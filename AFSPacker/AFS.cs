using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace AFSPacker
{
    static class AFS
    {
        const uint HEADER_MAGIC_1 = 0x00534641; // AFS
        const uint HEADER_MAGIC_2 = 0x20534641;
        const string NULL_FILE = "#NULL#";
        const string DUMMY_ENTRY_NAME_FOR_BLANK_RAW_NAME = "_NO_NAME";

        public enum NotificationTypes { Info, Warning, Error }

        public delegate void NotifyProgressDelegate(NotificationTypes type, string message);
        public static event NotifyProgressDelegate NotifyProgress;

        public static void CreateAFS(string inputDirectory, string outputFile, string filesList = null, bool preserveFileNames = true)
        {
            NotifyProgress?.Invoke(NotificationTypes.Info, "Packaging files...");

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

                uint currentOffset = Utils.Pad((uint)(8 + (8 * inputFiles.Length) + 8), 0x800);  // Header + TOC + AttributeTable Offset and size

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
                        currentOffset = Utils.Pad(currentOffset, 0x800);

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

                    if (preserveFileNames) NotifyProgress?.Invoke(NotificationTypes.Info, $"Processing TOC and attributes... {n}/{inputFiles.Length - 1}");
                    else NotifyProgress?.Invoke(NotificationTypes.Info, $"Processing TOC... {n}/{inputFiles.Length - 1}");
                }

                //Write TOC to file
                for (int n = 0; n < inputFiles.Length; n++)
                {
                    bw.Write(toc[n].Offset);
                    bw.Write(toc[n].FileSize);

                    NotifyProgress?.Invoke(NotificationTypes.Info, $"Writing TOC... {n}/{inputFiles.Length - 1}");
                }

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
                        fs1.Seek(toc[n].Offset, SeekOrigin.Begin);

                        using (FileStream fs = File.OpenRead(inputFiles[n]))
                        {
                            fs.CopyTo(fs1);
                        }
                    }

                    NotifyProgress?.Invoke(NotificationTypes.Info, $"Writing files... {n}/{inputFiles.Length - 1}");
                }

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

                        NotifyProgress?.Invoke(NotificationTypes.Info, $"Writing attributes... {n}/{inputFiles.Length - 1}");
                    }
                }

                //Pad final 0s
                long currentPosition = fs1.Position;
                long eof = Utils.Pad((uint)fs1.Position, 0x800);
                for (long n = currentPosition; n < eof; n++) bw.Write((byte)0);
            }
        }

        public static void ExtractAFS(string inputFile, string outputDirectory, string filesList = null)
        {
            using (FileStream fs1 = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs1))
            {
                uint magic = br.ReadUInt32();
                if (magic != HEADER_MAGIC_1 && magic != HEADER_MAGIC_2) //If Magic is different than AFS
                {
                    NotifyProgress?.Invoke(NotificationTypes.Error, "Input file doesn't seem to be a valid AFS file.");
                    return;
                }

                NotifyProgress?.Invoke(NotificationTypes.Info, "Extracting files...");

                uint numberOfFiles = br.ReadUInt32();

                TableOfContents[] toc = new TableOfContents[numberOfFiles];
                FileAttributes[] atrributes = new FileAttributes[numberOfFiles];

                //Read TOC
                for (int n = 0; n < numberOfFiles; n++)
                {
                    toc[n].Offset = br.ReadUInt32();
                    toc[n].FileSize = br.ReadUInt32();

                    NotifyProgress?.Invoke(NotificationTypes.Info, $"Reading TOC... {n}/{numberOfFiles - 1}");
                }

                //Read Filename Directory Offset and Size
                bool areThereAttributes = false;
                uint attributeTableOffset = 0;
                uint attributeTableSize = 0;

                while (fs1.Position < toc[0].Offset - 4)
                {
                    uint offset = br.ReadUInt32();
                    uint size = br.ReadUInt32();

                    // If zeroes are found, keep searching for attribute data.
                    // Sometimes it's at the begenning of the block and sometimes at the end.
                    if (offset == 0) continue;
                    if (size == 0) continue;

                    // Check if this data makes sense, as there are times where random data can be found
                    // instead of attribute offset and size. If not, let's assume there's no attribute data.
                    uint lastFileEndOffset = toc[numberOfFiles - 1].Offset + toc[numberOfFiles - 1].FileSize;
                    if (size > fs1.Length - lastFileEndOffset) break;
                    if (size < numberOfFiles * 0x30) break;
                    if (offset < lastFileEndOffset) break;
                    if (offset > fs1.Length - size) break;

                    // If the above conditions are not met, it looks like it's valid attribute data
                    areThereAttributes = true;
                    attributeTableOffset = offset;
                    attributeTableSize = size;
                    break;
                }

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

                        if (string.IsNullOrWhiteSpace(fileName[n]))
                        {
                            // The game "Winback 2 Project Poseidon" has attributes with empty file names.
                            // Give the files a dummy name for them to extract properly.
                            fileName[n] = DUMMY_ENTRY_NAME_FOR_BLANK_RAW_NAME;
                        }
                        else
                        {
                            // There are some cases where instead of a file name, an AFS file will store a truncated path like in Soul Calibur 2.
                            // Remove that path just in case to prevent from extracting into non-existing directories
                            fileName[n] = Path.GetFileName(fileName[n]);
                        }

                        atrributes[n].Year = br.ReadUInt16();
                        atrributes[n].Month = br.ReadUInt16();
                        atrributes[n].Day = br.ReadUInt16();
                        atrributes[n].Hour = br.ReadUInt16();
                        atrributes[n].Minute = br.ReadUInt16();
                        atrributes[n].Second = br.ReadUInt16();
                        atrributes[n].FileSize = br.ReadUInt32();

                        NotifyProgress?.Invoke(NotificationTypes.Info, $"Reading attributes table... {n}/{numberOfFiles - 1}");
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

                //Extract files
                if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);

                string[] filelist = new string[numberOfFiles];

                for (int n = 0; n < numberOfFiles; n++)
                {
                    if (toc[n].FileSize == 0 && toc[n].Offset == 0)
                    {
                        NotifyProgress?.Invoke(NotificationTypes.Warning, $"File \"{n}\" is a null file; Skipping.");

                        filelist[n] = NULL_FILE;

                        continue;
                    }

                    NotifyProgress?.Invoke(NotificationTypes.Info, $"Reading files... {n}/{numberOfFiles - 1}");

                    fs1.Seek(toc[n].Offset, SeekOrigin.Begin);

                    string outputFile = Path.Combine(outputDirectory, fileName[n]);
                    if (File.Exists(outputFile))
                    {
                        NotifyProgress?.Invoke(NotificationTypes.Warning, $"File \"{outputFile}\" already exists. Overwriting.");
                    }

                    using (FileStream fs = File.OpenWrite(outputFile))
                    {
                        fs1.CopySliceTo(fs, (int)toc[n].FileSize);
                    }

                    if (areThereAttributes)
                    {
                        try
                        {
                            DateTime date = new DateTime(atrributes[n].Year, atrributes[n].Month, atrributes[n].Day, atrributes[n].Hour, atrributes[n].Minute, atrributes[n].Second);
                            File.SetLastWriteTime(outputFile, date);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            NotifyProgress?.Invoke(NotificationTypes.Warning, "Invalid date. Ignoring.");
                        }
                    }

                    filelist[n] = outputFile; //Save the list of files in order to have the original order
                }

                if (!string.IsNullOrEmpty(filesList)) File.WriteAllLines(filesList, filelist);
            }
        }

        static string[] CheckForDuplicatedFilenames(string[] fileNames)
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