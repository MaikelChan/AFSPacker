using System;
using System.IO;

namespace AFSPacker
{
    static class Utils
    {
        public static uint Pad(uint value, uint padBytes)
        {
            if ((value % padBytes) != 0) return value + (padBytes - (value % padBytes));
            else return value;
        }

        public static void CopySliceTo(this Stream origin, Stream destination, int bytesCount)
        {
            byte[] buffer = new byte[65536];
            int count;

            while ((count = origin.Read(buffer, 0, Math.Min(buffer.Length, bytesCount))) != 0)
            {
                destination.Write(buffer, 0, count);
                bytesCount -= count;
            }
        }
    }
}