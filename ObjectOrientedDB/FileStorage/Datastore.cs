using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ObjectOrientedDB.FileStorage
{
    interface IDatastore
    {
        byte[] Read(long dataPosition, long dataSize);
        (long, Task) Insert(byte[] data);
        void Delete(long dataPosition, long dataSize);
    }

    class Datastore : IDatastore, IDisposable
    {
        private struct Metadata
        {
            public long NextOffset;
        }

        private readonly MemoryMappedFile file;
        private readonly MemoryMappedViewAccessor metadataAccessor;
        private Metadata metadata;

        public Datastore(MemoryMappedFile file)
        {
            this.file = file;

            var metadataSize = Marshal.SizeOf(typeof(Metadata));
            metadataAccessor = file.CreateViewAccessor(0, metadataSize);
            metadataAccessor.Read(0, out metadata);

            if (metadata.NextOffset == 0)
            {
                metadata.NextOffset = metadataSize;
                metadataAccessor.Write(0, ref metadata);
                metadataAccessor.Flush();
            }
        }

        public void Dispose()
        {
            metadataAccessor.Dispose();
            file.Dispose();
        }

        public void Delete(long dataPosition, long dataSize)
        {
            // NOP
            // in the future we can try to re-use the space maybe
        }

        // O(1)
        public (long, Task) Insert(byte[] data)
        {
            // update metadata
            var dataOffset = metadata.NextOffset;
            metadata.NextOffset += data.Length;
            metadataAccessor.Write(0, ref metadata);

            // save data
            var dataAccessor = file.CreateViewAccessor(dataOffset, data.Length);
            dataAccessor.WriteArray(0, data, 0, data.Length);

            // async flush tasks
            var flushMetadata = Task.Run(() => metadataAccessor.Flush());
            var flushData = Task.Run(() =>
            {
                dataAccessor.Flush();
                dataAccessor.Dispose();
            });
            var flushAll = Task.WhenAll(flushMetadata, flushData);

            return (dataOffset, flushAll);
        }

        // O(1)
        public byte[] Read(long dataPosition, long dataSize)
        {
            using (var dataAccessor = file.CreateViewAccessor(dataPosition, dataSize))
            {
                var data = new byte[dataSize];
                dataAccessor.ReadArray(0, data, 0, data.Length);
                return data;
            }
        }
    }
}
