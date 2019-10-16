using System;
using ObjectOrientedDB.FileStorage;

namespace ObjectOrientedDB
{
    public class ObjectDB<TGroup> : IDisposable
    {
        public delegate Guid GuidProvider();

        private readonly StorageEngine storageEngine;
        private readonly Serializer<TGroup> serializer;
        private readonly GuidProvider guidProvider;

        public ObjectDB(StorageEngine storageEngine, Serializer<TGroup> serializer, GuidProvider guidProvider = null)
        {
            this.storageEngine = storageEngine;
            this.serializer = serializer;
            this.guidProvider = guidProvider ?? (() => Guid.NewGuid());
        }

        public Guid Insert(TGroup obj)
        {
            var guid = guidProvider();
            var data = serializer.Serialize(obj);
            storageEngine.Insert(guid, data);
            return guid;
        }

        public T Read<T>(Guid guid) where T : TGroup
        {
            var data = storageEngine.Read(guid);
            var obj = serializer.Deserialize<T>(data);
            return obj;
        }

        public void Update(Guid guid, TGroup updated)
        {
            var data = serializer.Serialize(updated);
            storageEngine.Update(guid, data);
        }

        public void Delete(Guid guid)
        {
            storageEngine.Delete(guid);
        }

        public void Dispose()
        {
            if (storageEngine is IDisposable)
                ((IDisposable)storageEngine).Dispose();
        }
    }
}