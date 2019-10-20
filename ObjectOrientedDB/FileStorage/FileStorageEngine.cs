using System;
using System.IO.MemoryMappedFiles;

namespace ObjectOrientedDB.FileStorage
{
    /// <summary>
    /// <para>A StorageEngine implementation which uses file-based storage with memory-mapped files.</para>
    /// </summary>
    public class FileStorageEngine : StorageEngine, IDisposable
    {
        private readonly IIndex index;
        private readonly IDatastore datastore;

        public const long DEFAULT_INDEX_SIZE = 64;

        internal FileStorageEngine(IIndex index, IDatastore datastore)
        {
            this.index = index;
            this.datastore = datastore;
        }

        public void Dispose()
        {
            if (index is IDisposable)
                ((IDisposable)index).Dispose();
            if (datastore is IDisposable)
                ((IDisposable)datastore).Dispose();
        }

        /// <inheritdoc />
        public byte[] Read(Guid guid)
        {
            var indexEntry = index.Find(guid);
            var data = datastore.Read(indexEntry.DataOffset, indexEntry.DataSize);
            return data;
        }

        /// <inheritdoc />
        public void Insert(Guid guid, byte[] data)
        {
            // save data
            var insertDataResult = datastore.Insert(data);
            var dataOffset = insertDataResult.Item1;
            var flushData = insertDataResult.Item2;

            index.Insert(guid, dataOffset, data.Length);

            flushData.Wait();
        }

        /// <inheritdoc />
        public void Update(Guid guid, byte[] data)
        {

            var insertDataResult = datastore.Insert(data);
            var newDataOffset = insertDataResult.Item1;
            var flushDataTask = insertDataResult.Item2;

            index.Update(guid, newDataOffset);
            flushDataTask.Wait();
        }

        /// <inheritdoc />
        public void Delete(Guid guid)
        {
            var deletedEntry = index.Delete(guid);
            datastore.Delete(deletedEntry.DataOffset, deletedEntry.DataSize);
        }

    }
}