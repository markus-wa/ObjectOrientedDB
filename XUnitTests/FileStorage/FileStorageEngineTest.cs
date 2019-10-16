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

        [Fact]
        public void CreateAddsMetadata()
        {
            using (var indexFile = MemoryMappedFile.CreateNew("index", SIZE_1MB))
            using (new FileStorageEngine(indexFile, MemoryMappedFile.CreateNew("data", SIZE_1MB)))
            using (var metadataAccessor = indexFile.CreateViewAccessor(0, Marshal.SizeOf(typeof(Metadata))))
            {
                metadataAccessor.Read(0, out Metadata metadata);
                Assert.Equal(0, metadata.Data.NextOffset);
            }
        }

        private FileStorageEngine Create1MBEngine()
        {
            return new FileStorageEngine(MemoryMappedFile.CreateNew("index", SIZE_1MB), MemoryMappedFile.CreateNew("data", SIZE_1MB));
        }

        [Fact]
        public void StoreUpdatesMetadata()
        {
            using (var indexFile = MemoryMappedFile.CreateNew("index", SIZE_1MB))
            using (var engine = new FileStorageEngine(indexFile, MemoryMappedFile.CreateNew("data", SIZE_1MB)))
            {
                engine.Insert(Guid.NewGuid(), BitConverter.GetBytes(UInt64.MaxValue));

                using (var metadataAccessor = indexFile.CreateViewAccessor(0, Marshal.SizeOf(typeof(Metadata))))
                {
                    metadataAccessor.Read(0, out Metadata metadata);
                    Assert.Equal(1, metadata.Index.NextBSTNode);
                    Assert.Equal(8, metadata.Data.NextOffset);
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

                var bstOffset = Marshal.SizeOf(typeof(Metadata));
                var bstNodeSize = Marshal.SizeOf(typeof(BSTNode));
                using (var bstAccessor = indexFile.CreateViewAccessor(bstOffset, 3 * bstNodeSize))
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

                using (var dataAccessor = dataFile.CreateViewAccessor(0, input.Length))
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

        [Fact]
        public void Bench()
        {
            var n = 10 * 1000;
            var nThreads = 1; // multithreading is currently not supported
            using (var engine = FileStorageEngine.Create("db", 1024 * SIZE_1MB, n * nThreads))
            {
                byte data = 1;

                var input = new byte[] { data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,
                        data++, data++, data++, data++, data++, data++, data++, data++,};

                Task[] taskArray = new Task[nThreads];
                for (int i = 0; i < taskArray.Length; i++)
                {
                    taskArray[i] = Task.Run(() =>
                    {
                        for (var j = 0; j < n; j++)
                        {
                            var guid = Guid.NewGuid();
                            engine.Insert(guid, input);
                            var output = engine.Read(guid);

                            Assert.Equal(input, output);
                        }
                    });
                }
                Task.WaitAll(taskArray);
            }
        }
    }
}
