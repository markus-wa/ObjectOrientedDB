using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace ObjectOrientedDB.FileStorage
{
    public class FileStorageEngineTest
    {
        const long SIZE_1MB = 1024 * 1024;
        const long INDEX_METADATA_SIZE = 8;
        const long DATA_METADATA_SIZE = 8;

        [Fact]
        public void StoreUpdatesMetadata()
        {
            using (var indexFile = MemoryMappedFile.CreateNew("index", SIZE_1MB))
            using (var dataFile = MemoryMappedFile.CreateNew("data", SIZE_1MB))
            using (var engine = new FileStorageEngine(indexFile, dataFile))
            {
                engine.Insert(Guid.NewGuid(), BitConverter.GetBytes(UInt64.MaxValue));

                using (var indexMetadataAccessor = indexFile.CreateViewAccessor(0, INDEX_METADATA_SIZE))
                {
                    var nextBstNode = indexMetadataAccessor.ReadInt64(0);
                    Assert.Equal(1, nextBstNode);
                }

                using (var dataMetadataAccessor = dataFile.CreateViewAccessor(0, INDEX_METADATA_SIZE))
                {
                    var nextBstNode = dataMetadataAccessor.ReadInt64(0);
                    Assert.Equal(DATA_METADATA_SIZE + 8, nextBstNode);
                }
            }
        }

        [Fact]
        public void StoreUpdatesBST()
        {
            using (var indexFile = MemoryMappedFile.CreateNew("index", SIZE_1MB))
            using (var engine = new FileStorageEngine(indexFile, MemoryMappedFile.CreateNew("data", SIZE_1MB)))
            {
                byte i = 0;
                Func<Guid> guidGenerator = () => new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, i++);
                engine.Insert(guidGenerator(), BitConverter.GetBytes(UInt64.MaxValue));
                engine.Insert(guidGenerator(), BitConverter.GetBytes(Int64.MinValue));
                engine.Insert(guidGenerator(), BitConverter.GetBytes(Int64.MaxValue));

                var bstNodeSize = Marshal.SizeOf(typeof(BSTNode));
                using (var bstAccessor = indexFile.CreateViewAccessor(INDEX_METADATA_SIZE, 3 * bstNodeSize))
                {
                    // root
                    bstAccessor.Read(0, out BSTNode bstNode);
                    Assert.Equal(0, bstNode.Low);
                    Assert.Equal(1, bstNode.High);

                    // high
                    bstAccessor.Read(bstNodeSize, out bstNode);
                    Assert.Equal(0, bstNode.Low);
                    Assert.Equal(2, bstNode.High);

                    // high high
                    bstAccessor.Read(2 * bstNodeSize, out bstNode);
                    Assert.Equal(0, bstNode.Low);
                    Assert.Equal(0, bstNode.High);
                }
            }
        }

        [Fact]
        public void StoreSavesData()
        {
            using (var dataFile = MemoryMappedFile.CreateNew("data", SIZE_1MB))
            using (var engine = new FileStorageEngine(MemoryMappedFile.CreateNew("index", SIZE_1MB), dataFile))
            {
                var input = BitConverter.GetBytes(UInt64.MaxValue);
                engine.Insert(Guid.NewGuid(), input);

                using (var dataAccessor = dataFile.CreateViewAccessor(DATA_METADATA_SIZE, input.Length))
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
            using (var dataFile = MemoryMappedFile.CreateNew("data", SIZE_1MB))
            using (var engine = new FileStorageEngine(MemoryMappedFile.CreateNew("index", SIZE_1MB), dataFile))
            {
                var input = new byte[] { 1, 2, 3, 4 };
                var guid = Guid.NewGuid();
                engine.Insert(guid, input);
                var output = engine.Read(guid);

                Assert.Equal(input, output);
            }
        }

        [Fact]
        public void StoreReadTwo()
        {
            using (var engine = new FileStorageEngine(MemoryMappedFile.CreateNew("index", SIZE_1MB), MemoryMappedFile.CreateNew("data", SIZE_1MB)))
            {
                var input = new byte[] { 1, 2, 3, 4 };
                var guid = Guid.NewGuid();
                engine.Insert(guid, input);
                var output = engine.Read(guid);

                Assert.Equal(input, output);

                input = new byte[] { 5, 6, 7, 8 };
                guid = Guid.NewGuid();
                engine.Insert(guid, input);
                output = engine.Read(guid);

                Assert.Equal(input, output);
            }
        }

        [Fact]
        public void StoreReadMultiple()
        {
            using (var engine = new FileStorageEngine(MemoryMappedFile.CreateNew("index", SIZE_1MB), MemoryMappedFile.CreateNew("data", SIZE_1MB)))
            {
                byte data = 1;

                for (var i = 0; i < FileStorageEngine.DEFAULT_INDEX_SIZE; i++)
                {
                    var input = new byte[] { data++, data++, data++, data++ };
                    var guid = Guid.NewGuid();
                    engine.Insert(guid, input);
                    var output = engine.Read(guid);

                    Assert.Equal(input, output);
                }
            }
        }
    }
}
