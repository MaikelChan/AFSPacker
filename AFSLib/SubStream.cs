using System;
using System.IO;

namespace AFSLib
{
    class SubStream : Stream
    {
        private Stream baseStream;
        private readonly long origin;
        private readonly long length;
        private readonly bool leaveBaseStreamOpen;

        public SubStream(Stream baseStream, long offset, long length, bool leaveBaseStreamOpen)
        {
            if (baseStream == null) throw new ArgumentNullException(nameof(baseStream));
            if (!baseStream.CanRead) throw new ArgumentException("Base stream is not readable.");
            if (!baseStream.CanSeek) throw new ArgumentException("Base stream is non seekable.");
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));

            origin = baseStream.Position + offset;
            if (baseStream.Length - origin < length) throw new ArgumentException("Offset or length arguments would try to read past the end of base stream.");

            this.baseStream = baseStream;
            this.length = length;
            this.leaveBaseStreamOpen = leaveBaseStreamOpen;

            Position = 0;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckDisposed();

            long remaining = length - Position;
            if (remaining <= 0) return 0;
            if (remaining < count) count = (int)remaining;
            return baseStream.Read(buffer, offset, count);
        }

        private void CheckDisposed()
        {
            if (baseStream == null) throw new ObjectDisposedException(GetType().Name);
        }

        public override long Length
        {
            get { CheckDisposed(); return length; }
        }

        public override bool CanRead
        {
            get { CheckDisposed(); return true; }
        }

        public override bool CanWrite
        {
            get { CheckDisposed(); return false; }
        }

        public override bool CanSeek
        {
            get { CheckDisposed(); return true; }
        }

        public override long Position
        {
            get
            {
                CheckDisposed();

                return baseStream.Position - origin;
            }
            set
            {
                CheckDisposed();

                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
                if (value >= length) throw new ArgumentOutOfRangeException(nameof(value));
                baseStream.Position = origin + value;
            }
        }

        public override long Seek(long offset, SeekOrigin seekOrigin)
        {
            CheckDisposed();

            if (offset > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(offset));

            switch (seekOrigin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = length - offset;
                    break;
                default:
                    throw new ArgumentException("Invalid seekOrigin.");
            }

            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
            CheckDisposed();
            baseStream.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (baseStream != null)
                {
                    if (!leaveBaseStreamOpen)
                    {
                        try { baseStream.Dispose(); }
                        catch { }
                    }

                    baseStream = null;
                }
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}