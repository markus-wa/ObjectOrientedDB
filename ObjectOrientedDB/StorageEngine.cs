using System;

namespace ObjectOrientedDB.FileStorage
{
    public interface StorageEngine
    {
        Guid Store(byte[] data);
        byte[] Read(Guid guid);
    }
}