
namespace AFSLib
{
    public enum HeaderMagicType
    {
        /// <summary>
        /// Some AFS files contain a 4-byte header magic with 'AFS' followed by 0x00.
        /// </summary>
        AFS_00,

        /// <summary>
        /// Some AFS files contain a 4-byte header magic with 'AFS' followed by 0x20.
        /// </summary>
        AFS_20
    }
}