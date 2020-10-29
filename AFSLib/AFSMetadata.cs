using System.Text.Json.Serialization;

namespace AFSLib
{
    class AFSMetadata
    {
        public uint MetadataVersion { get; set; }
        public HeaderTypes HeaderType { get; set; }
        public AttributesTypes AttributesType { get; set; }
        public string[] FileNames { get; set; }

        public enum HeaderTypes { AFS_00, AFS_20 }
        public enum AttributesTypes { NoAttributes, InfoAtBeginning, InfoAtEnd }

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

        const uint CURRENT_VERSION = 1;

        public AFSMetadata()
        {
            MetadataVersion = CURRENT_VERSION;
            HeaderType = HeaderTypes.AFS_00;
            AttributesType = AttributesTypes.InfoAtBeginning;
            FileNames = null;
        }
    }
}