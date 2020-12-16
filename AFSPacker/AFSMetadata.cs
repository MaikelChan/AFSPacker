using AFSLib;
using System;
using System.IO;
using System.Text.Json;

namespace AFSPacker
{
    class AFSMetadata
    {
        public uint MetadataVersion { get; set; }

        public HeaderMagicType HeaderMagicType { get; set; }
        public AttributesInfoType AttributesInfoType { get; set; }
        public AFSMetadataEntry[] Entries { get; set; }

        const uint CURRENT_VERSION = 1;

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
            Entries = new AFSMetadataEntry[afs.EntryCount];

            for (int e = 0; e < afs.EntryCount; e++)
            {
                Entries[e] = new AFSMetadataEntry()
                {
                    RawName = afs.Entries[e].RawName,
                    Name = afs.Entries[e].Name
                };
            }
        }

        public void SaveToFile(string metadataFileNamePath)
        {
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                WriteIndented = true
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

            return JsonSerializer.Deserialize<AFSMetadata>(metadataContents, options);
        }
    }
}