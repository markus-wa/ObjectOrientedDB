using System;

namespace ObjectOrientedDB.FileStorage
{
    /// <summary>
    /// <para>Represents an entry in the Binary Search Tree index of a FileStorageEngine</para>
    /// </summary>
    struct IndexEntry
    {
        public Guid Guid;

        public long DataOffset;

        public long DataSize;

        public long Low;
        public long High;

        public IndexEntry(Guid guid, long dataOffset, long size)
        {
            Guid = guid;
            DataOffset = dataOffset;
            DataSize = size;
            Low = 0;
            High = 0;
        }
    }
}