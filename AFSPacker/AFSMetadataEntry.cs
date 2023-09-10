
namespace AFSPacker
{
    public class AFSMetadataEntry
    {
        public bool IsNull { get; set; } = false;
        public string Name { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public uint CustomData { get; set; } = 0;

        #region Deprecated

        public bool? HasUnknownAttribute { get; set; } = null;
        public string RawName { get; set; } = null;
        public uint? UnknownAttribute { get; set; } = null;

        #endregion
    }
}