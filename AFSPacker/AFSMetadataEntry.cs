
namespace AFSPacker
{
    public class AFSMetadataEntry
    {
        public bool IsNull { get; set; } = false;
        public string Name { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public bool HasUnknownAttribute { get; set; } = false;
        public uint UnknownAttribute { get; set; } = 0;

        #region Deprecated

        public string RawName { get; set; } = null;

        #endregion
    }
}