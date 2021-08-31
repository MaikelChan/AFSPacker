using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace AFSLib
{
    public class AFS
    {
        /// <summary>
        /// Each of the entries in the AFS object.
        /// </summary>
        public ReadOnlyCollection<Entry> Entries => readonlyEntries;

        /// <summary>
        /// The header magic that the AFS file will have.
        /// </summary>
        public HeaderMagicType HeaderMagicType { get; set; }

        /// <summary>
        /// The location where the attributes info will be stored. Or if the file won't contain any attributes.
        /// </summary>
        public AttributesInfoType AttributesInfoType { get; set; }

        /// <summary>
        /// The amount of entries in this AFS object.
        /// </summary>
        public uint EntryCount => (uint)entries.Count;

        /// <summary>
        /// If the AFS object contains attributes or not. It will be false if AttributesInfoType == AttributesInfoType.NoAttributes.
        /// </summary>
        public bool ContainsAttributes => AttributesInfoType != AttributesInfoType.NoAttributes;

        /// <summary>
        /// Event that will be called each time some process wants to report something.
        /// </summary>
        public event NotifyProgressDelegate NotifyProgress;
        public delegate void NotifyProgressDelegate(NotificationType type, string message);

        internal const uint HEADER_MAGIC_00 = 0x00534641; // AFS
        internal const uint HEADER_MAGIC_20 = 0x20534641;
        internal const uint HEADER_SIZE = 0x8;
        internal const uint ENTRY_INFO_ELEMENT_SIZE = 0x8;
        internal const uint ATTRIBUTE_INFO_SIZE = 0x8;
        internal const uint ATTRIBUTE_ELEMENT_SIZE = 0x30;
        internal const uint MAX_ENTRY_NAME_LENGTH = 0x20;
        internal const uint PADDING_SIZE = 0x800;

        internal const string DUMMY_ENTRY_NAME_FOR_BLANK_RAW_NAME = "_NO_NAME";

        private readonly Stream afsStream;

        private readonly List<Entry> entries;
        private readonly ReadOnlyCollection<Entry> readonlyEntries;
        private readonly Dictionary<string, uint> duplicates;
        private readonly string[] invalidPathChars;

        /// <summary>
        /// Create an empty AFS object.
        /// </summary>
        public AFS()
        {
            entries = new List<Entry>();
            readonlyEntries = entries.AsReadOnly();
            duplicates = new Dictionary<string, uint>();

            char[] chars = Path.GetInvalidPathChars();
            invalidPathChars = new string[chars.Length];
            for (int ipc = 0; ipc < chars.Length; ipc++)
            {
                invalidPathChars[ipc] = chars[ipc].ToString();
            }

            HeaderMagicType = HeaderMagicType.AFS_00;
            AttributesInfoType = AttributesInfoType.InfoAtBeginning;
        }

        /// <summary>
        /// Create an AFS object out of an AFS stream.
        /// </summary>
        /// <param name="afsStream">Stream containing the AFS file data.</param>
        public AFS(Stream afsStream) : this()
        {
            if (afsStream == null)
            {
                throw new ArgumentNullException(nameof(afsStream));
            }

            this.afsStream = afsStream;

            using (BinaryReader br = new BinaryReader(afsStream, Encoding.UTF8, true))
            {
                // Check if the Magic is valid

                uint magic = br.ReadUInt32();

                if (magic == HEADER_MAGIC_00)
                {
                    HeaderMagicType = HeaderMagicType.AFS_00;
                }
                else if (magic == HEADER_MAGIC_20)
                {
                    HeaderMagicType = HeaderMagicType.AFS_20;
                }
                else
                {
                    throw new InvalidDataException("Stream doesn't seem to contain valid AFS data.");
                }

                // Start gathering info about entries and attributes

                uint entryCount = br.ReadUInt32();
                StreamEntryInfo[] entriesInfo = new StreamEntryInfo[entryCount];

                uint dataBlockStartOffset = 0;
                uint dataBlockEndOffset = 0;

                for (int e = 0; e < entryCount; e++)
                {
                    entriesInfo[e].Offset = br.ReadUInt32();
                    entriesInfo[e].Size = br.ReadUInt32();

                    if (entriesInfo[e].IsNull)
                    {
                        continue;
                    }

                    if (dataBlockStartOffset == 0) dataBlockStartOffset = entriesInfo[e].Offset;
                    dataBlockEndOffset = entriesInfo[e].Offset + entriesInfo[e].Size;
                }

                // Find where attribute info is located

                AttributesInfoType = AttributesInfoType.NoAttributes;

                uint attributeDataOffset = br.ReadUInt32();
                uint attributeDataSize = br.ReadUInt32();

                bool isAttributeInfoValid = IsAttributeInfoValid(attributeDataOffset, attributeDataSize, (uint)afsStream.Length, dataBlockEndOffset);

                if (isAttributeInfoValid)
                {
                    AttributesInfoType = AttributesInfoType.InfoAtBeginning;
                }
                else
                {
                    afsStream.Position = dataBlockStartOffset - ATTRIBUTE_INFO_SIZE;
                    attributeDataOffset = br.ReadUInt32();
                    attributeDataSize = br.ReadUInt32();

                    isAttributeInfoValid = IsAttributeInfoValid(attributeDataOffset, attributeDataSize, (uint)afsStream.Length, dataBlockEndOffset);

                    if (isAttributeInfoValid)
                    {
                        AttributesInfoType = AttributesInfoType.InfoAtEnd;
                    }
                }

                // Read attribute data if there is any

                if (ContainsAttributes)
                {
                    afsStream.Position = attributeDataOffset;

                    for (int e = 0; e < entryCount; e++)
                    {
                        if (entriesInfo[e].IsNull)
                        {
                            // It's a null entry, so ignore attribute data

                            afsStream.Position += ATTRIBUTE_ELEMENT_SIZE;

                            continue;
                        }
                        else
                        {
                            // It's a valid entry, so read attribute data

                            byte[] name = new byte[MAX_ENTRY_NAME_LENGTH];
                            afsStream.Read(name, 0, name.Length);

                            entriesInfo[e].Name = Utils.GetStringFromBytes(name);
                            entriesInfo[e].LastWriteTime = new DateTime(br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt16());
                            entriesInfo[e].Unknown = br.ReadUInt32();
                        }
                    }
                }
                else
                {
                    for (int e = 0; e < entryCount; e++)
                    {
                        entriesInfo[e].Name = $"{e:00000000}";
                    }
                }

                // After gathering all necessary info, create the entries.

                for (int e = 0; e < entryCount; e++)
                {
                    StreamEntry entry = entriesInfo[e].IsNull ? null : new StreamEntry(this, afsStream, entriesInfo[e]);
                    entries.Add(entry);
                }

                UpdateEntriesNames();
            }
        }

        /// <summary>
        /// Saves the contents of this AFS object into a stream.
        /// </summary>
        /// <param name="outputStream">The stream where the data is going to be saved.</param>
        public void SaveToStream(Stream outputStream)
        {
            if (outputStream == null)
            {
                throw new ArgumentNullException(nameof(outputStream));
            }

            if (outputStream == afsStream)
            {
                throw new ArgumentException("Can't save into the same stream the AFS data is being read from.", nameof(outputStream));
            }

            // Start creating the AFS file

            NotifyProgress?.Invoke(NotificationType.Info, "Creating AFS stream...");

            using (BinaryWriter bw = new BinaryWriter(outputStream))
            {
                bw.Write(HeaderMagicType == HeaderMagicType.AFS_20 ? HEADER_MAGIC_20 : HEADER_MAGIC_00);
                bw.Write(EntryCount);

                // Calculate the offset of each entry

                uint[] offsets = new uint[EntryCount];

                uint firstEntryOffset = Utils.Pad(HEADER_SIZE + (ENTRY_INFO_ELEMENT_SIZE * EntryCount) + ATTRIBUTE_INFO_SIZE, PADDING_SIZE);
                uint currentEntryOffset = firstEntryOffset;

                for (int e = 0; e < EntryCount; e++)
                {
                    if (entries[e] == null)
                    {
                        offsets[e] = 0;
                    }
                    else
                    {
                        offsets[e] = currentEntryOffset;

                        currentEntryOffset += entries[e].Size;
                        currentEntryOffset = Utils.Pad(currentEntryOffset, PADDING_SIZE);
                    }
                }

                // Write entries info

                for (int e = 0; e < EntryCount; e++)
                {
                    NotifyProgress?.Invoke(NotificationType.Info, $"Writing entry info... {e + 1}/{EntryCount}");

                    if (entries[e] == null)
                    {
                        bw.Write((uint)0);
                        bw.Write((uint)0);
                    }
                    else
                    {
                        bw.Write(offsets[e]);
                        bw.Write(entries[e].Size);
                    }
                }

                // Write attributes info if available

                outputStream.Position = HEADER_SIZE + (EntryCount * ENTRY_INFO_ELEMENT_SIZE);
                Utils.FillStreamWithZeroes(outputStream, firstEntryOffset - (uint)outputStream.Position);

                uint attributesInfoOffset = currentEntryOffset;

                if (ContainsAttributes)
                {
                    if (AttributesInfoType == AttributesInfoType.InfoAtBeginning)
                        outputStream.Position = HEADER_SIZE + (EntryCount * ENTRY_INFO_ELEMENT_SIZE);
                    else if (AttributesInfoType == AttributesInfoType.InfoAtEnd)
                        outputStream.Position = firstEntryOffset - ATTRIBUTE_INFO_SIZE;

                    bw.Write(attributesInfoOffset);
                    bw.Write(EntryCount * ATTRIBUTE_ELEMENT_SIZE);
                }

                // Write entries data to stream

                for (int e = 0; e < EntryCount; e++)
                {
                    if (entries[e] == null)
                    {
                        NotifyProgress?.Invoke(NotificationType.Info, $"Null file... {e + 1}/{EntryCount}");
                    }
                    else
                    {
                        NotifyProgress?.Invoke(NotificationType.Info, $"Writing entry... {e + 1}/{EntryCount}");

                        outputStream.Position = offsets[e];

                        using (Stream entryStream = entries[e].GetStream())
                        {
                            entryStream.CopyTo(outputStream);
                        }
                    }
                }

                // Write attributes if available

                if (ContainsAttributes)
                {
                    outputStream.Position = attributesInfoOffset;

                    for (int e = 0; e < EntryCount; e++)
                    {
                        if (entries[e] == null)
                        {
                            NotifyProgress?.Invoke(NotificationType.Info, $"Null file... {e + 1}/{EntryCount}");

                            outputStream.Position += ATTRIBUTE_ELEMENT_SIZE;
                        }
                        else
                        {
                            NotifyProgress?.Invoke(NotificationType.Info, $"Writing attribute... {e + 1}/{EntryCount}");

                            byte[] name = Encoding.Default.GetBytes(entries[e].RawName);
                            outputStream.Write(name, 0, name.Length);
                            outputStream.Position += MAX_ENTRY_NAME_LENGTH - name.Length;

                            bw.Write((ushort)entries[e].LastWriteTime.Year);
                            bw.Write((ushort)entries[e].LastWriteTime.Month);
                            bw.Write((ushort)entries[e].LastWriteTime.Day);
                            bw.Write((ushort)entries[e].LastWriteTime.Hour);
                            bw.Write((ushort)entries[e].LastWriteTime.Minute);
                            bw.Write((ushort)entries[e].LastWriteTime.Second);
                            bw.Write(entries[e].Unknown);
                        }
                    }
                }

                // Pad final zeroes

                uint currentPosition = (uint)outputStream.Position;
                uint endOfFile = Utils.Pad(currentPosition, PADDING_SIZE);
                Utils.FillStreamWithZeroes(outputStream, endOfFile - currentPosition);

                // Make sure the stream is the size of the AFS data (in case the stream was bigger)

                outputStream.SetLength(endOfFile);
            }

            NotifyProgress?.Invoke(NotificationType.Success, "AFS stream has been saved successfully.");
        }

        /// <summary>
        /// Adds a new entry from a file.
        /// </summary>
        /// <param name="fileNamePath">Path to the file that will be added.</param>
        /// <param name="entryName">The name of the entry.</param>
        public void AddEntryFromFile(string fileNamePath, string entryName)
        {
            if (string.IsNullOrEmpty(fileNamePath))
            {
                throw new ArgumentNullException(nameof(entryName));
            }

            if (!File.Exists(fileNamePath))
            {
                throw new FileNotFoundException($"File \"{fileNamePath}\" has not been found.", fileNamePath);
            }

            if (entryName == null)
            {
                throw new ArgumentNullException(nameof(entryName));
            }

            entries.Add(new FileEntry(this, fileNamePath, entryName));
            UpdateEntriesNames();
        }

        /// <summary>
        /// Removes an entry from the AFS object.
        /// </summary>
        /// <param name="entry">The entry to remove.</param>
        public void RemoveEntry(Entry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            if (entries.Contains(entry))
            {
                entries.Remove(entry);
                UpdateEntriesNames();
            }
        }

        /// <summary>
        /// Extracts one entry to a file.
        /// </summary>
        /// <param name="entry">The entry to extract.</param>
        /// <param name="outputFilePath">The path to the file where the entry will be saved. If it doesn't exist, it will be created.</param>
        public void ExtractEntry(Entry entry, string outputFilePath)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            if (string.IsNullOrEmpty(outputFilePath))
            {
                throw new ArgumentNullException(nameof(outputFilePath));
            }

            string directory = Path.GetDirectoryName(outputFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (FileStream outputStream = File.Create(outputFilePath))
            using (Stream entryStream = entry.GetStream())
            {
                entryStream.CopyTo(outputStream);
            }

            if (ContainsAttributes)
            {
                File.SetLastWriteTime(outputFilePath, entry.LastWriteTime);
            }
        }

        /// <summary>
        /// Extracts all the entries from the AFS object.
        /// </summary>
        /// <param name="outputDirectory">The directory where the entries will be saved. If it doesn't exist, it will be created.</param>
        public void ExtractAllEntries(string outputDirectory)
        {
            if (string.IsNullOrEmpty(outputDirectory))
            {
                throw new ArgumentNullException(nameof(outputDirectory));
            }

            if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);

            for (int e = 0; e < EntryCount; e++)
            {
                if (entries[e] == null)
                {
                    NotifyProgress?.Invoke(NotificationType.Warning, $"Null entry. Skipping... {e + 1}/{EntryCount}");
                    continue;
                }

                NotifyProgress?.Invoke(NotificationType.Info, $"Extracting entry... {e + 1}/{EntryCount}");

                string outputFilePath = Path.Combine(outputDirectory, entries[e].Name);
                if (File.Exists(outputFilePath))
                {
                    NotifyProgress?.Invoke(NotificationType.Warning, $"File \"{outputFilePath}\" already exists. Overwriting...");
                }

                ExtractEntry(entries[e], outputFilePath);
            }

            NotifyProgress?.Invoke(NotificationType.Success, $"Finished extracting all entries successfully.");
        }

        /// <summary>
        /// Updates the names of all the entries to be unique in case of duplicates and cleans them up to not contain invalid characters.
        /// </summary>
        internal void UpdateEntriesNames()
        {
            // There can be multiple files with the same name, so keep track of duplicates

            duplicates.Clear();

            for (int e = 0; e < EntryCount; e++)
            {
                if (entries[e] == null) continue;

                string cleanedUpName = CleanUpName(entries[e].RawName);

                bool found = duplicates.TryGetValue(cleanedUpName, out uint duplicateCount);

                if (found) duplicates[cleanedUpName] = ++duplicateCount;
                else duplicates.Add(cleanedUpName, 0);

                if (duplicateCount > 0)
                {
                    string nameWithoutExtension = Path.ChangeExtension(cleanedUpName, null);
                    string nameDuplicate = $" ({duplicateCount})";
                    string nameExtension = Path.GetExtension(cleanedUpName);

                    cleanedUpName = nameWithoutExtension + nameDuplicate + nameExtension;
                }

                entries[e].UpdateName(cleanedUpName);
            }
        }

        private bool IsAttributeInfoValid(uint attributesOffset, uint attributesSize, uint afsFileSize, uint dataBlockEndOffset)
        {
            // If zeroes are found, info is not valid.
            if (attributesOffset == 0) return false;
            if (attributesSize == 0) return false;

            // Check if this info makes sense, as there are times where random
            // data can be found instead of attribute offset and size.
            if (attributesSize > afsFileSize - dataBlockEndOffset) return false;
            if (attributesSize < EntryCount * ATTRIBUTE_ELEMENT_SIZE) return false;
            if (attributesOffset < dataBlockEndOffset) return false;
            if (attributesOffset > afsFileSize - attributesSize) return false;

            // If the above conditions are not met, it looks like it's valid attribute data
            return true;
        }

        private string CleanUpName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                // The game "Winback 2: Project Poseidon" has attributes with empty file names.
                // Give the files a dummy name for them to extract properly.
                return DUMMY_ENTRY_NAME_FOR_BLANK_RAW_NAME;
            }

            // There are some cases where instead of a file name, an AFS file will store a path, like in Soul Calibur 2 or Crimson Tears.
            // Let's make sure there aren't any invalid characters in the path so the OS doesn't complain.

            string cleanedUpName = name;

            for (int ipc = 0; ipc < invalidPathChars.Length; ipc++)
            {
                cleanedUpName = cleanedUpName.Replace(invalidPathChars[ipc], string.Empty);
            }

            // Also remove any ":" in case there are drive letters in the path (like, again, in Soul Calibur 2)

            cleanedUpName = cleanedUpName.Replace(":", string.Empty);

            return cleanedUpName;
        }
    }
}