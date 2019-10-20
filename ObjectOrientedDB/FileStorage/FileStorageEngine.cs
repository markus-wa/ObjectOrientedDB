using System;
using System.IO.MemoryMappedFiles;

namespace ObjectOrientedDB.FileStorage
{
    public class FileStorageEngine : StorageEngine, IDisposable
    {
        private readonly IIndex index;
        private readonly IDatastore datastore;

        public const long DEFAULT_INDEX_SIZE = 64;

        public FileStorageEngine(IIndex index, IDatastore datastore)
        {
            this.index = index;
            this.datastore = datastore;
        }

        public FileStorageEngine(MemoryMappedFile indexFile, MemoryMappedFile dataFile)
        {
            this.index = new Index(indexFile);
            this.datastore = new Datastore(dataFile);
        }

        public void Dispose()
        {
            if (index is IDisposable)
                ((IDisposable)index).Dispose();
            if (datastore is IDisposable)
                ((IDisposable)datastore).Dispose();
        }

        public byte[] Read(Guid guid)
        {
            var indexEntry = index.Find(guid);
            var data = datastore.Read(indexEntry.DataOffset, indexEntry.DataSize);
            return data;
        }

        public void Insert(Guid guid, byte[] data)
        {
            // save data
            var insertDataResult = datastore.Insert(data);
            var dataOffset = insertDataResult.Item1;
            var flushData = insertDataResult.Item2;

            index.Insert(guid, dataOffset, data.Length);

            flushData.Wait();
        }

        public void Update(Guid guid, byte[] data)
        {

            var insertDataResult = datastore.Insert(data);
            var newDataOffset = insertDataResult.Item1;
            var flushDataTask = insertDataResult.Item2;

            index.Update(guid, newDataOffset);
            flushDataTask.Wait();
        }

        public void Delete(Guid guid)
        {
            var deletedEntry = index.Delete(guid);
            datastore.Delete(deletedEntry.DataOffset, deletedEntry.DataSize);
        }

    }
}