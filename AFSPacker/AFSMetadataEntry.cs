using System.Text.Json.Serialization;

namespace AFSPacker
{
    public class AFSMetadataEntry
    {
        public bool IsNull { get; set; } = false;
        public string Name { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;

        #region Deprecated

        public string RawName { get; set; } = null;

        #endregion
    }
}