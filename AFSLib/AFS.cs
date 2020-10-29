using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace AFSLib
{
    public static class AFS
    {
        const uint HEADER_MAGIC_00 = 0x00534641; // AFS
        const uint HEADER_MAGIC_20 = 0x20534641;

        public enum NotificationTypes { Info, Warning, Error, Success }

        public delegate void NotifyProgressDelegate(NotificationTypes type, string message);
        public static event NotifyProgressDelegate NotifyProgress;

        /// <summary>
        /// Creates an AFS file out of the files inside a directory.
        /// </summary>
        /// <param name="inputDirectory">Directory containing the files that will go into the AFS file.</param>
        /// <param name="outputFile">Path to the AFS file to create.</param>
        public static void CreateAFS(string inputDirectory, string outputFile)
        {
            if (string.IsNullOrEmpty(inputDirectory))
            {
                throw new ArgumentNullException(nameof(inputDirectory));
            }

            if (!Directory.Exists(inputDirectory))
            {
                throw new DirectoryNotFoundException("The following directory has not been found: " + inputDirectory);
            }

            if (string.IsNullOrEmpty(outputFile))
            {
                throw new ArgumentNullException(nameof(outputFile));
            }

            // Read metadata, or create a default one if it doesn't exist

            string metadataFile = inputDirectory + ".json";
            AFSMetadata metadata;

            if (File.Exists(metadataFile))
            {
                metadata = AFSMetadata.LoadFromFile(metadataFile);
            }
            else
            {
                NotifyProgress?.Invoke(NotificationTypes.Warning, $"Metadata file has not been found: \"{metadataFile}\". Creating an AFS file with default settings.");

                metadata = new AFSMetadata(inputDirectory);
            }

            // Start creating the AFS file

            NotifyProgress?.Invoke(NotificationTypes.Info, "Packaging files...");

            using (FileStream fs1 = File.Create(outputFile))
            using (BinaryWriter bw = new BinaryWriter(fs1))
            {
                bw.Write(metadata.HeaderType == AFSMetadata.HeaderTypes.AFS_20 ? HEADER_MAGIC_20 : HEADER_MAGIC_00);
                bw.Write((uint)metadata.FileCount);

                // Generate TOC and FileNameDirectory

                TableOfContents[] toc = new TableOfContents[metadata.FileCount];
                FileAttributes[] attributes = new FileAttributes[metadata.FileCount];

                uint currentOffset = Utils.Pad((uint)(8 + (8 * metadata.FileCount) + 8), 0x800);  // Header + TOC + AttributeTable Offset and size

                for (int f = 0; f < metadata.FileCount; f++)
                {
                    if (metadata.FileNames[f] == string.Empty)
                    {
                        toc[f].FileSize = 0;
                        toc[f].Offset = 0;

                        if (metadata.ContainsAttributes)
                        {
                            attributes[f].FileName = string.Empty;
                            attributes[f].Year = 0;
                            attributes[f].Month = 0;
                            attributes[f].Day = 0;
                            attributes[f].Hour = 0;
                            attributes[f].Minute = 0;
                            attributes[f].Second = 0;
                            attributes[f].FileSize = 0;
                        }
                    }
                    else
                    {
                        string fileName = Path.Combine(inputDirectory, metadata.FileNames[f]);
                        FileInfo fileInfo = new FileInfo(fileName);

                        toc[f].FileSize = (uint)fileInfo.Length;
                        toc[f].Offset = currentOffset;

                        currentOffset += toc[f].FileSize;
                        currentOffset = Utils.Pad(currentOffset, 0x800);

                        if (metadata.ContainsAttributes)
                        {
                            attributes[f].FileName = metadata.FileNames[f];
                            attributes[f].Year = (ushort)fileInfo.LastWriteTime.Year;
                            attributes[f].Month = (ushort)fileInfo.LastWriteTime.Month;
                            attributes[f].Day = (ushort)fileInfo.LastWriteTime.Day;
                            attributes[f].Hour = (ushort)fileInfo.LastWriteTime.Hour;
                            attributes[f].Minute = (ushort)fileInfo.LastWriteTime.Minute;
                            attributes[f].Second = (ushort)fileInfo.LastWriteTime.Second;
                            attributes[f].FileSize = (uint)fileInfo.Length;
                        }
                    }

                    if (metadata.ContainsAttributes)
                    {
                        NotifyProgress?.Invoke(NotificationTypes.Info, $"Processing TOC and attributes... {f + 1}/{metadata.FileCount}");
                    }
                    else
                    {
                        NotifyProgress?.Invoke(NotificationTypes.Info, $"Processing TOC... {f + 1}/{metadata.FileCount}");
                    }
                }

                // Write TOC to file

                for (int f = 0; f < metadata.FileCount; f++)
                {
                    bw.Write(toc[f].Offset);
                    bw.Write(toc[f].FileSize);

                    NotifyProgress?.Invoke(NotificationTypes.Info, $"Writing TOC... {f + 1}/{metadata.FileCount}");
                }

                uint attributeTableOffset = 0;
                uint attributeTableSize = 0;

                if (metadata.ContainsAttributes)
                {
                    // Write Filename Directory Offset and Size

                    attributeTableOffset = currentOffset;
                    attributeTableSize = (uint)(metadata.FileCount * 0x30);

                    if (metadata.AttributesType == AFSMetadata.AttributesTypes.InfoAtBeginning)
                        fs1.Seek(8 + (metadata.FileCount * 8), SeekOrigin.Begin);
                    else if (metadata.AttributesType == AFSMetadata.AttributesTypes.InfoAtEnd)
                        fs1.Seek(toc[0].Offset - 8, SeekOrigin.Begin);

                    bw.Write(attributeTableOffset);
                    bw.Write(attributeTableSize);
                }

                // Write files data to file

                for (int f = 0; f < metadata.FileCount; f++)
                {
                    if (metadata.FileNames[f] != string.Empty)
                    {
                        fs1.Seek(toc[f].Offset, SeekOrigin.Begin);

                        using (FileStream fs = File.OpenRead(Path.Combine(inputDirectory, metadata.FileNames[f])))
                        {
                            fs.CopyTo(fs1);
                        }

                        NotifyProgress?.Invoke(NotificationTypes.Info, $"Writing files... {f + 1}/{metadata.FileCount}");
                    }
                    else
                    {
                        NotifyProgress?.Invoke(NotificationTypes.Info, $"Null file... {f + 1}/{metadata.FileCount}");
                    }
                }

                if (metadata.ContainsAttributes)
                {
                    //Write Filename Directory
                    fs1.Seek(attributeTableOffset, SeekOrigin.Begin);
                    for (int f = 0; f < metadata.FileCount; f++)
                    {
                        byte[] name = Encoding.Default.GetBytes(attributes[f].FileName);
                        fs1.Write(name, 0, name.Length);
                        fs1.Seek(0x20 - name.Length, SeekOrigin.Current);

                        bw.Write(attributes[f].Year);
                        bw.Write(attributes[f].Month);
                        bw.Write(attributes[f].Day);
                        bw.Write(attributes[f].Hour);
                        bw.Write(attributes[f].Minute);
                        bw.Write(attributes[f].Second);
                        bw.Write(attributes[f].FileSize);

                        NotifyProgress?.Invoke(NotificationTypes.Info, $"Writing attributes... {f + 1}/{metadata.FileCount}");
                    }
                }

                //Pad final 0s
                long currentPosition = fs1.Position;
                long eof = Utils.Pad((uint)fs1.Position, 0x800);
                for (long n = currentPosition; n < eof; n++) bw.Write((byte)0);
            }

            NotifyProgress?.Invoke(NotificationTypes.Success, $"\"{Path.GetFileName(outputFile)}\" has been created successfully.");
        }

        /// <summary>
        /// Extracts the contents of an AFS into the specified directory.
        /// </summary>
        /// <param name="inputFile">Path to the AFS file to extract from.</param>
        /// <param name="inputDirectory">Directory that will contain the extracted files.</param>
        public static void ExtractAFS(string inputFile, string outputDirectory)
        {
            if (string.IsNullOrEmpty(inputFile))
            {
                throw new ArgumentNullException(nameof(inputFile));
            }

            if (!File.Exists(inputFile))
            {
                throw new FileNotFoundException("The following file has not been found: " + inputFile);
            }

            if (string.IsNullOrEmpty(outputDirectory))
            {
                throw new ArgumentNullException(nameof(outputDirectory));
            }

            AFSMetadata metadata = new AFSMetadata();

            using (FileStream fs1 = File.OpenRead(inputFile))
            using (BinaryReader br = new BinaryReader(fs1))
            {
                uint magic = br.ReadUInt32();

                if (magic == HEADER_MAGIC_00)
                {
                    metadata.HeaderType = AFSMetadata.HeaderTypes.AFS_00;
                }
                else if (magic == HEADER_MAGIC_20)
                {
                    metadata.HeaderType = AFSMetadata.HeaderTypes.AFS_20;
                }
                else
                {
                    NotifyProgress?.Invoke(NotificationTypes.Error, "Input file doesn't seem to be a valid AFS file.");
                    return;
                }

                NotifyProgress?.Invoke(NotificationTypes.Info, "Extracting files...");

                uint fileCount = br.ReadUInt32();

                TableOfContents[] toc = new TableOfContents[fileCount];
                FileAttributes[] atrributes = new FileAttributes[fileCount];

                // Read TOC

                for (int f = 0; f < fileCount; f++)
                {
                    toc[f].Offset = br.ReadUInt32();
                    toc[f].FileSize = br.ReadUInt32();

                    NotifyProgress?.Invoke(NotificationTypes.Info, $"Reading TOC... {f + 1}/{fileCount}");
                }

                // Read Attributes Table Offset and Size

                metadata.AttributesType = AFSMetadata.AttributesTypes.NoAttributes;

                uint lastFileEndOffset = toc[fileCount - 1].Offset + toc[fileCount - 1].FileSize;
                uint attributeTableOffset = br.ReadUInt32();
                uint attributeTableSize = br.ReadUInt32();

                bool isAttributeInfoValid = IsAttributeInfoValid(attributeTableOffset, attributeTableSize, (uint)fs1.Length, fileCount, lastFileEndOffset);

                if (isAttributeInfoValid)
                {
                    metadata.AttributesType = AFSMetadata.AttributesTypes.InfoAtBeginning;
                }
                else
                {
                    fs1.Position = toc[0].Offset - 8;
                    attributeTableOffset = br.ReadUInt32();
                    attributeTableSize = br.ReadUInt32();

                    isAttributeInfoValid = IsAttributeInfoValid(attributeTableOffset, attributeTableSize, (uint)fs1.Length, fileCount, lastFileEndOffset);

                    if (isAttributeInfoValid)
                    {
                        metadata.AttributesType = AFSMetadata.AttributesTypes.InfoAtEnd;
                    }
                }

                if (metadata.ContainsAttributes) NotifyProgress?.Invoke(NotificationTypes.Info, $"Attributes table found at 0x{attributeTableOffset:X8}.");
                else NotifyProgress?.Invoke(NotificationTypes.Info, "Attributes table not found.");

                string[] fileNames = new string[fileCount];

                if (metadata.ContainsAttributes)
                {
                    // Read Attribute table

                    fs1.Seek(attributeTableOffset, SeekOrigin.Begin);

                    for (int f = 0; f < fileCount; f++)
                    {
                        if (toc[f].IsNullEntry)
                        {
                            NotifyProgress?.Invoke(NotificationTypes.Warning, $"Null entry. Skipping... {f + 1}/{fileCount}");

                            fileNames[f] = string.Empty;
                            fs1.Seek(0x30, SeekOrigin.Current);

                            continue;
                        }
                        else
                        {
                            byte[] name = new byte[32];
                            fs1.Read(name, 0, name.Length);
                            fileNames[f] = Encoding.Default.GetString(name).Replace("\0", "");

                            atrributes[f].Year = br.ReadUInt16();
                            atrributes[f].Month = br.ReadUInt16();
                            atrributes[f].Day = br.ReadUInt16();
                            atrributes[f].Hour = br.ReadUInt16();
                            atrributes[f].Minute = br.ReadUInt16();
                            atrributes[f].Second = br.ReadUInt16();
                            atrributes[f].FileSize = br.ReadUInt32();

                            NotifyProgress?.Invoke(NotificationTypes.Info, $"Reading attributes table... {f + 1}/{fileCount}");
                        }
                    }

                    fileNames = CheckForDuplicatedFilenames(fileNames);
                }
                else
                {
                    for (int f = 0; f < fileCount; f++)
                    {
                        fileNames[f] = f.ToString("00000000", CultureInfo.InvariantCulture);
                    }
                }

                metadata.FileNames = fileNames;

                // Extract files

                if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);

                for (int f = 0; f < fileCount; f++)
                {
                    if (toc[f].IsNullEntry)
                    {
                        NotifyProgress?.Invoke(NotificationTypes.Warning, $"Null entry. Skipping... {f + 1}/{fileCount}");
                        continue;
                    }

                    NotifyProgress?.Invoke(NotificationTypes.Info, $"Reading files... {f + 1}/{fileCount}");

                    fs1.Seek(toc[f].Offset, SeekOrigin.Begin);

                    string outputFile = Path.Combine(outputDirectory, fileNames[f]);
                    if (File.Exists(outputFile))
                    {
                        NotifyProgress?.Invoke(NotificationTypes.Warning, $"File \"{outputFile}\" already exists. Overwriting.");
                    }

                    using (FileStream fs = File.OpenWrite(outputFile))
                    {
                        fs1.CopySliceTo(fs, (int)toc[f].FileSize);
                    }

                    if (metadata.ContainsAttributes)
                    {
                        try
                        {
                            DateTime date = new DateTime(atrributes[f].Year, atrributes[f].Month, atrributes[f].Day, atrributes[f].Hour, atrributes[f].Minute, atrributes[f].Second);
                            File.SetLastWriteTime(outputFile, date);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            NotifyProgress?.Invoke(NotificationTypes.Warning, "Invalid date. Ignoring.");
                        }
                    }
                }
            }

            metadata.SaveToFile(outputDirectory + ".json");

            NotifyProgress?.Invoke(NotificationTypes.Success, $"\"{Path.GetFileName(inputFile)}\" has been extracted successfully.");
        }

        static string[] CheckForDuplicatedFilenames(string[] fileNames)
        {
            string[] output = new string[fileNames.Length];

            for (int f = 0; f < fileNames.Length; f++)
            {
                if (string.IsNullOrEmpty(fileNames[f]))
                {
                    output[f] = string.Empty;
                    continue;
                }

                int count = 0;

                for (int o = 0; o < f; o++)
                {
                    if (fileNames[f] == fileNames[o]) count++;
                }

                if (count == 0) output[f] = fileNames[f];
                else output[f] = $"{Path.GetFileNameWithoutExtension(fileNames[f])} ({count}){Path.GetExtension(fileNames[f])}";
            }

            return output;
        }

        static bool IsAttributeInfoValid(uint attributesOffset, uint attributesSize, uint afsFileSize, uint fileCount, uint lastFileEndOffset)
        {
            // If zeroes are found, info is not valid.
            if (attributesOffset == 0) return false;
            if (attributesSize == 0) return false;

            // Check if this info makes sense, as there are times where random
            // data can be found instead of attribute offset and size.
            if (attributesSize > afsFileSize - lastFileEndOffset) return false;
            if (attributesSize < fileCount * 0x30) return false;
            if (attributesOffset < lastFileEndOffset) return false;
            if (attributesOffset > afsFileSize - attributesSize) return false;

            // If the above conditions are not met, it looks like it's valid attribute data
            return true;
        }

        struct TableOfContents
        {
            public uint Offset;
            public uint FileSize;

            public bool IsNullEntry { get { return Offset == 0 && FileSize == 0; } }
        }

        struct FileAttributes
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