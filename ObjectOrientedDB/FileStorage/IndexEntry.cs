using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ObjectOrientedDB.FileStorage
{
    public struct IndexEntry
    {
        private long GuidPart1;
        private long GuidPart2;

        public long DataOffset;

        public long Size;

        public IndexEntry(Guid guid, long dataOffset, long size) : this()
        {
            var guidBytes = guid.ToByteArray();
            GuidPart1 = BitConverter.ToInt64(guidBytes, 0);
            GuidPart2 = BitConverter.ToInt64(guidBytes, 8);
            DataOffset = dataOffset;
            Size = size;
        }

        public Guid Guid()
        {
            var a1 = BitConverter.GetBytes(GuidPart1);
            var a2 = BitConverter.GetBytes(GuidPart2);
            byte[] bytes = new byte[16];
            Buffer.BlockCopy(a1, 0, bytes, 0, 8);
            Buffer.BlockCopy(a2, 0, bytes, 8, 8);
            return new Guid(bytes);
        }
    }
}