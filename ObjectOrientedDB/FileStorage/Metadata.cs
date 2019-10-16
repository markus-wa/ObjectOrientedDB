﻿namespace ObjectOrientedDB.FileStorage
{
    public struct Metadata
    {
        public IndexMetadata Index;
        public DataMetadata Data;

        public struct IndexMetadata
        {
            public long NextBSTNode;
        }

        public struct DataMetadata
        {
            public long NextOffset;
        }
    }
}