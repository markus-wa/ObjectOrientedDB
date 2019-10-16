using System;

namespace ObjectOrientedDB
{
    public interface StorageEngine
    {
        byte[] Read(Guid guid);
        void Insert(Guid guid, byte[] data);
        void Update(Guid guid, byte[] data);
        void Delete(Guid guid);
    }
}