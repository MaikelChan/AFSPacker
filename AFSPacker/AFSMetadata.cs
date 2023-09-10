using AFSLib;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AFSPacker
{
    class AFSMetadata
    {
        public uint MetadataVersion { get; set; }

        public HeaderMagicType HeaderMagicType { get; set; }
        public AttributesInfoType AttributesInfoType { get; set; }
        public bool AllAttributesContainEntrySize { get; set; }
        public uint EntryBlockAlignment { get; set; }
        public AFSMetadataEntry[] Entries { get; set; }

        const uint CURRENT_VERSION = 3;

        public AFSMetadata()
        {

        }

        public AFSMetadata(AFS afs)
        {
            if (afs == null)
            {
                throw new ArgumentNullException(nameof(afs));
            }

            MetadataVersion = CURRENT_VERSION;

            HeaderMagicType = afs.HeaderMagicType;
            AttributesInfoType = afs.AttributesInfoType;
            AllAttributesContainEntrySize = afs.AllAttributesContainEntrySize;
            EntryBlockAlignment = afs.EntryBlockAlignment;
            Entries = new AFSMetadataEntry[afs.EntryCount];

            for (int e = 0; e < afs.EntryCount; e++)
            {
                if (afs.Entries[e] is NullEntry)
                {
                    Entries[e] = new AFSMetadataEntry()
                    {
                        IsNull = true,
                        Name = string.Empty,
                        FileName = string.Empty,
                        CustomData = 0
                    };
                }
                else
                {
                    DataEntry dataEntry = afs.Entries[e] as DataEntry;

                    Entries[e] = new AFSMetadataEntry()
                    {
                        IsNull = false,
                        Name = dataEntry.Name,
                        FileName = dataEntry.SanitizedName,
                        CustomData = dataEntry.CustomData
                    };
                }
            }
        }

        public void SaveToFile(string metadataFileNamePath)
        {
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            string metadataContents = JsonSerializer.Serialize(this, options);
            File.WriteAllText(metadataFileNamePath, metadataContents);
        }

        public static AFSMetadata LoadFromFile(string metadataFileNamePath)
        {
            if (string.IsNullOrEmpty(metadataFileNamePath))
            {
                throw new ArgumentNullException(nameof(metadataFileNamePath));
            }

            string metadataContents = File.ReadAllText(metadataFileNamePath);

            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            AFSMetadata metadata = JsonSerializer.Deserialize<AFSMetadata>(metadataContents, options);
            bool hasBeenMigrated = metadata.Migrate(Path.ChangeExtension(metadataFileNamePath, null));

            if (hasBeenMigrated)
            {
                metadata.SaveToFile(metadataFileNamePath);
            }

            return metadata;
        }

        #region Metadata Migration

        Action<string>[] migrationMethods;

        private bool Migrate(string metadataPath)
        {
            if (MetadataVersion == CURRENT_VERSION) return false;

            if (migrationMethods == null)
            {
                migrationMethods = new Action<string>[]
                {
                    Migrate_1_2,
                    Migrate_2_3
                };
            }

            for (uint v = MetadataVersion - 1; v < CURRENT_VERSION - 1; v++)
            {
                migrationMethods[v](metadataPath);
            }

            MetadataVersion = CURRENT_VERSION;

            return true;
        }

        /// <summary>
        /// Added EntryBlockAlignment and NullEntry support.
        /// Added HasUnknownAttribute and UnknownAttribute properties.
        /// Renamed Name to FileName and RawName to Name.
        /// </summary>
        void Migrate_1_2(string metadataPath)
        {
            EntryBlockAlignment = 0x800;

            for (int e = 0; e < Entries.Length; e++)
            {
                Entries[e].IsNull = false;
                Entries[e].FileName = Entries[e].Name;
                Entries[e].Name = Entries[e].RawName;
                Entries[e].RawName = null;
                Entries[e].HasUnknownAttribute = false;
                Entries[e].UnknownAttribute = 0;
            }
        }

        /// <summary>
        /// Renamed UnkownAttribute to CustomData.
        /// Deleted HasUnknownAttribute.
        /// Added AttributesContainEntrySize property.
        /// </summary>
        void Migrate_2_3(string metadataPath)
        {
            bool allAttributesContainEntrySize = true;

            for (int e = 0; e < Entries.Length; e++)
            {
                if (Entries[e].HasUnknownAttribute.HasValue && Entries[e].HasUnknownAttribute.Value)
                {
                    allAttributesContainEntrySize = false;
                    Entries[e].CustomData = Entries[e].UnknownAttribute.HasValue ? Entries[e].UnknownAttribute.Value : 0;
                }
                else
                {
                    string entryFilePath = Path.Combine(metadataPath, Entries[e].FileName);
                    FileInfo info = new FileInfo(entryFilePath);
                    Entries[e].CustomData = (uint)info.Length;
                }

                Entries[e].UnknownAttribute = null;
                Entries[e].HasUnknownAttribute = null;
            }

            AllAttributesContainEntrySize = allAttributesContainEntrySize;
        }

        #endregion
    }
}