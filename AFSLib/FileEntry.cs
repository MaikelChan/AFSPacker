using System.IO;

namespace AFSLib
{
    public class FileEntry : Entry
    {
        private readonly FileInfo fileInfo;

        internal FileEntry(AFS afs, string fileNamePath) : base(afs)
        {
            fileInfo = new FileInfo(fileNamePath);

            name = fileInfo.Name;
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