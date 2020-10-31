using System;
using System.IO;
using System.Text;

namespace AFSLib
{
    internal static class Utils
    {
        internal static uint Pad(uint value, uint padBytes)
        {
            if ((value % padBytes) != 0) return value + (padBytes - (value % padBytes));
            else return value;
        }

        internal static void FillStreamWithZeroes(Stream stream, uint length)
        {
            byte[] padding = new byte[length];
            stream.Write(padding, 0, (int)length);
        }

        internal static void CopySliceTo(this Stream origin, Stream destination, int bytesCount)
        {
            byte[] buffer = new byte[65536];
            int count;

            while ((count = origin.Read(buffer, 0, Math.Min(buffer.Length, bytesCount))) != 0)
            {
                destination.Write(buffer, 0, count);
                bytesCount -= count;
            }
        }

        internal static string GetStringFromBytes(byte[] bytes)
        {
            return Encoding.Default.GetString(bytes).Replace("\0", "");
        }
    }
}