using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AFSLib
{
    class AFSMetadata
    {
        public uint MetadataVersion { get; set; }
        public HeaderTypes HeaderType { get; set; }
        public AttributesTypes AttributesType { get; set; }
        public string[] FileNames { get; set; }

        [JsonIgnore]
        public int FileCount
        {
            get
            {
                if (FileNames == null) return 0;
                return FileNames.Length;
            }
        }

        [JsonIgnore]
        public bool ContainsAttributes
        {
            get
            {
                return AttributesType != AttributesTypes.NoAttributes;
            }
        }

        public enum HeaderTypes { AFS_00, AFS_20 }
        public enum AttributesTypes { NoAttributes, InfoAtBeginning, InfoAtEnd }

        const uint CURRENT_VERSION = 1;

        public AFSMetadata() : this(null)
        {

        }

        public AFSMetadata(string filesDirectory)
        {
            MetadataVersion = CURRENT_VERSION;
            HeaderType = HeaderTypes.AFS_00;
            AttributesType = AttributesTypes.InfoAtBeginning;
            FileNames = null;

            if (filesDirectory != null)
            {
                string[] fileNames = Directory.GetFiles(filesDirectory);

                // Make sure that fileNames don't contain any path, just the name.
                for (int f = 0; f < fileNames.Length; f++)
                {
                    fileNames[f] = Path.GetFileName(fileNames[f]);
                }

                FileNames = fileNames;
            }
        }

        public static AFSMetadata LoadFromFile(string metadataFileName)
        {
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            string metadataContents = File.ReadAllText(metadataFileName);
            return JsonSerializer.Deserialize<AFSMetadata>(metadataContents, options);
        }

        public void SaveToFile(string metadataFileName)
        {
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                WriteIndented = true
            };

            string metadataContents = JsonSerializer.Serialize(this, options);
            File.WriteAllText(metadataFileName, metadataContents);
        }
    }
}