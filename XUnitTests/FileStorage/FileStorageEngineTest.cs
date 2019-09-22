using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Xunit;

namespace ObjectOrientedDB.FileStorage
{
    public class FileStorageEngineTest
    {
        const long SIZE_1MB = 1024 * 1024;

        [Fact]
        public void StoreReturnsGuid()
        {
            using (var mmf = MemoryMappedFile.CreateNew("db", SIZE_1MB))
            {
                var expectedGuid = Guid.NewGuid();

                StorageEngine engine = new FileStorageEngine(mmf, () => expectedGuid);
                var guid = engine.Store(BitConverter.GetBytes(UInt64.MaxValue));

                Assert.Equal(expectedGuid, guid);
            }
        }

        [Fact]
        public void CreateAddsMetadata()
        {
            using (var mmf = MemoryMappedFile.CreateNew("db", SIZE_1MB))
            {
                StorageEngine engine = new FileStorageEngine(mmf);

                using (var metadataAccessor = mmf.CreateViewAccessor(0, Marshal.SizeOf(typeof(Metadata))))
                {
                    Metadata metadata;
                    metadataAccessor.Read(0, out metadata);
                    Assert.Equal(64, metadata.Index.Size);
                    Assert.Equal(0, metadata.Index.NextEntry);
                    Assert.Equal(0, metadata.Data.NextOffset);
                }
            }
        }

        [Fact]
        public void StoreUpdatesMetadata()
        {
            using (var mmf = MemoryMappedFile.CreateNew("db", SIZE_1MB))
            {
                StorageEngine engine = new FileStorageEngine(mmf);
                var guid = engine.Store(BitConverter.GetBytes(UInt64.MaxValue));

                using (var metadataAccessor = mmf.CreateViewAccessor(0, Marshal.SizeOf(typeof(Metadata))))
                {
                    Metadata metadata;
                    metadataAccessor.Read(0, out metadata);
                    Assert.Equal(64, metadata.Index.Size);
                    Assert.Equal(1, metadata.Index.NextEntry);
                    Assert.Equal(8, metadata.Data.NextOffset);
                }
            }
        }

        [Fact]
        public void StoreAddsIndexEntry()
        {
            using (var mmf = MemoryMappedFile.CreateNew("db", SIZE_1MB))
            {
                StorageEngine engine = new FileStorageEngine(mmf);
                var guid = engine.Store(BitConverter.GetBytes(UInt64.MaxValue));

                using (var indexAccessor = mmf.CreateViewAccessor(Marshal.SizeOf(typeof(Metadata)), Marshal.SizeOf(typeof(IndexEntry))))
                {
                    IndexEntry indexEntry;
                    indexAccessor.Read(0, out indexEntry);
                    Assert.Equal(guid, indexEntry.Guid());
                    Assert.Equal(0, indexEntry.DataOffset);
                    Assert.Equal(8, indexEntry.Size);
                }
            }
        }

        [Fact]
        public void StoreUpdatesBST()
        {
            using (var mmf = MemoryMappedFile.CreateNew("db", SIZE_1MB))
            {
                byte i = 0;
                StorageEngine engine = new FileStorageEngine(mmf, () => new Guid(1, 2, 3, 4, 5, 6, 6, 7, 8, 9, i++));
                engine.Store(BitConverter.GetBytes(UInt64.MaxValue));
                engine.Store(BitConverter.GetBytes(Int64.MinValue));

                var bstOffset = Marshal.SizeOf(typeof(Metadata)) + FileStorageEngine.DEFAULT_INDEX_SIZE * Marshal.SizeOf(typeof(IndexEntry));
                var bstNodeSize = Marshal.SizeOf(typeof(BSTNode));
                using (var bstAccessor = mmf.CreateViewAccessor(bstOffset, 3 * bstNodeSize))
                {
                    // root
                    BSTNode bstNode;
                    bstAccessor.Read(0, out bstNode);
                    Assert.Equal(1, bstNode.Low);
                    Assert.Equal(2, bstNode.High);

                    // high
                    bstAccessor.Read(2 * bstNodeSize, out bstNode);
                    Assert.Equal(1, bstNode.Low);
                    Assert.Equal(1, bstNode.High);

                    // low
                    bstAccessor.Read(bstNodeSize, out bstNode);
                    Assert.Equal(0, bstNode.Low);
                    Assert.Equal(0, bstNode.High);
                }
            }
        }

        [Fact]
        public void StoreSavesData()
        {
            using (var mmf = MemoryMappedFile.CreateNew("db", SIZE_1MB))
            {
                StorageEngine engine = new FileStorageEngine(mmf);
                var input = BitConverter.GetBytes(UInt64.MaxValue);
                var guid = engine.Store(input);

                var indexEntrySize = Marshal.SizeOf(typeof(IndexEntry));
                var indexSize = FileStorageEngine.DEFAULT_INDEX_SIZE * indexEntrySize;
                var bstNodeSize = Marshal.SizeOf(typeof(BSTNode));
                var bstSize = 2 * FileStorageEngine.DEFAULT_INDEX_SIZE * bstNodeSize;
                var metadataSize = Marshal.SizeOf(typeof(Metadata));
                var dataOffset = metadataSize + indexSize + bstSize;
                using (var dataAccessor = mmf.CreateViewAccessor(dataOffset, input.Length))
                {
                    var stored = new byte[input.Length];
                    dataAccessor.ReadArray(0, stored, 0, stored.Length);
                    Assert.Equal(input, stored);
                }
            }
        }

        [Fact]
        public void StoreReadSingle()
        {
            using (var mmf = MemoryMappedFile.CreateNew("db", SIZE_1MB))
            {
                StorageEngine engine = new FileStorageEngine(mmf);
                byte data = 1;

                var input = new byte[] { 1, 2, 3, 4 };
                var output = engine.Read(engine.Store(input));

                Assert.Equal(input, output);
            }
        }

        [Fact]
        public void StoreReadTwo()
        {
            using (var mmf = MemoryMappedFile.CreateNew("db", SIZE_1MB))
            {
                StorageEngine engine = new FileStorageEngine(mmf);
                byte data = 1;

                var input = new byte[] { 1, 2, 3, 4 };
                var output = engine.Read(engine.Store(input));

                Assert.Equal(input, output);

                input = new byte[] { 5, 6, 7, 8 };
                output = engine.Read(engine.Store(input));

                Assert.Equal(input, output);
            }
        }

        [Fact]
        public void StoreReadMultiple()
        {
            using (var mmf = MemoryMappedFile.CreateNew("db", SIZE_1MB))
            {
                StorageEngine engine = new FileStorageEngine(mmf);
                byte data = 1;

                for (var i = 0; i < FileStorageEngine.DEFAULT_INDEX_SIZE - 10; i++)
                {
                    var input = new byte[] { data++, data++, data++, data++ };
                    var output = engine.Read(engine.Store(input));

                    Assert.Equal(input, output);
                }
            }
        }
    }
}
