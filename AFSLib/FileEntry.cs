using System.IO;

namespace AFSLib
{
    public class FileEntry : Entry
    {
        private readonly FileInfo fileInfo;

        internal FileEntry(AFS afs, string fileNamePath, string entryName) : base(afs)
        {
            fileInfo = new FileInfo(fileNamePath);

            rawName = entryName;
            size = (uint)fileInfo.Length;
            lastWriteTime = fileInfo.LastWriteTime;
            unknown = (uint)fileInfo.Length;
        }

        internal override Stream GetStream()
        {
            return fileInfo.OpenRead();
        }
    }
}