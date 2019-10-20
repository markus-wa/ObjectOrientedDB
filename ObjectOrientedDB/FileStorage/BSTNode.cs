using System;

namespace ObjectOrientedDB.FileStorage
{
    public struct BSTNode
    {
        public Guid Guid;

        public long DataOffset;

        public long DataSize;

        public long Low;
        public long High;

        public BSTNode(Guid guid, long dataOffset, long size)
        {
            this.Guid = guid;
            this.DataOffset = dataOffset;
            this.DataSize = size;
            this.Low = 0;
            this.High = 0;
        }
    }
}