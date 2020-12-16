using System.IO;

namespace AFSLib
{
    public class StreamEntry : Entry
    {
        private readonly Stream baseStream;
        private readonly uint baseStreamDataOffset;

        internal StreamEntry(AFS afs, Stream baseStream, StreamEntryInfo info) : base(afs)
        {
            this.baseStream = baseStream;
            baseStreamDataOffset = info.Offset;

            rawName = info.Name;
            size = info.Size;
            lastWriteTime = info.LastWriteTime;
            unknown = info.Unknown;
        }

        internal override Stream GetStream()
        {
            baseStream.Position = baseStreamDataOffset;
            return new SubStream(baseStream, 0, Size, true);
        }
    }
}