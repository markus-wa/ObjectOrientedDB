using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ObjectOrientedDB.FileStorage
{
    public class FileStorageEngine : StorageEngine, IDisposable
    {
        public delegate Guid GuidProvider();

        private readonly MemoryMappedFile indexFile;
        private readonly MemoryMappedFile dataFile;
        private readonly MemoryMappedViewAccessor metadataAccessor;
        private readonly long bstPosition;
        private readonly long bstNodeSize;
        private Metadata metadata;

        public const long DEFAULT_INDEX_SIZE = 64;

        public FileStorageEngine(MemoryMappedFile indexFile, MemoryMappedFile dataFile)
        {
            this.indexFile = indexFile;
            this.dataFile = dataFile;

            // read metadata
            var metadataSize = Marshal.SizeOf(typeof(Metadata));
            metadataAccessor = indexFile.CreateViewAccessor(0, metadataSize);
            metadataAccessor.Read(0, out metadata);

            // BST
            bstPosition = metadataSize;

            // data
            bstNodeSize = Marshal.SizeOf(typeof(BSTNode));
        }

        public void Dispose()
        {
            metadataAccessor.Dispose();
            indexFile.Dispose();
            dataFile.Dispose();
        }

        public static FileStorageEngine Create(string path, long dataBytes, long indexSize)
        {
            var indexBytes = Marshal.SizeOf(typeof(Metadata)) + indexSize * Marshal.SizeOf(typeof(BSTNode));
            Directory.CreateDirectory(path);
            var indexFile = CreateFile(path + "/index", indexBytes);
            var dataFile = CreateFile(path + "/data", dataBytes);
            return new FileStorageEngine(indexFile, dataFile);
        }

        private static MemoryMappedFile CreateFile(string path, long size)
        {
            return MemoryMappedFile.CreateFromFile(path, FileMode.Create, path, size);
        }

        public static FileStorageEngine Open(string path)
        {
            var indexFile = OpenFile(path + "/index");
            var dataFile = OpenFile(path + "/data");
            return new FileStorageEngine(indexFile, dataFile);
        }

        private static MemoryMappedFile OpenFile(string path)
        {
            return MemoryMappedFile.CreateFromFile(path, FileMode.Open, path);
        }

        public byte[] Read(Guid guid)
        {
            // search BST
            BSTNode bstNode = ClosestNode(guid).Item1;

            if (!Equals(guid, bstNode.Guid))
            {
                throw new RecordNotFoundException("entry for guid not found");
            }

            if (bstNode.DataOffset < 0)
            {
                throw new RecordNotFoundException("entry deleted");
            }

            // read data
            var data = new byte[bstNode.Size];
            using (var dataAccessor = dataFile.CreateViewAccessor(bstNode.DataOffset, data.Length))
            {
                dataAccessor.ReadArray(0, data, 0, data.Length);
            }
            return data;
        }

        private (BSTNode, long) ClosestNode(Guid guid)
        {
            BSTNode bstNode;
            long bstNodeOffset = 0;

            using (var indexAccessor = indexFile.CreateViewAccessor(bstPosition, 0))
            {
                while (true)
                {
                    indexAccessor.Read(bstNodeOffset, out bstNode);

                    var compRes = guid.CompareTo(bstNode.Guid);
                    if (compRes == 0)
                    {
                        // exact match found
                        return (bstNode, bstNodeOffset);
                    }

                    long nextNodeId = compRes < 0 ? bstNode.Low : bstNode.High;

                    if (nextNodeId == 0)
                    {
                        // insert point found
                        return (bstNode, bstNodeOffset);
                    }

                    bstNodeOffset = nextNodeId * bstNodeSize;
                }
            }
        }

        public void Insert(Guid guid, byte[] data)
        {
            // save data
            var insertDataResult = InsertData(data);
            var dataOffset = insertDataResult.Item1;
            var flushData = insertDataResult.Item2;

            using (var indexAccessor = indexFile.CreateViewAccessor(bstPosition, 0))
            {
                // update BST
                var closestMatch = ClosestNode(guid);
                var parentNode = closestMatch.Item1;
                var parentNodeOffset = closestMatch.Item2;

                Task flushMetadata;
                // TODO: not happy with this check
                if (metadata.Index.NextBSTNode == 0)
                {
                    // needed to handle root node / first entry correctly
                    metadata.Index.NextBSTNode = 1;
                    metadataAccessor.Write(0, ref metadata);
                    flushMetadata = Task.Run(() => metadataAccessor.Flush());

                    parentNode.Guid = guid;
                    parentNode.DataOffset = dataOffset;
                    parentNode.Size = data.Length;
                }
                else
                {
                    // normal path
                    long newNodeId = metadata.Index.NextBSTNode;
                    metadata.Index.NextBSTNode++;
                    metadataAccessor.Write(0, ref metadata);
                    flushMetadata = Task.Run(() => metadataAccessor.Flush());

                    var compRes = guid.CompareTo(parentNode.Guid);
                    if (compRes < 0)
                    {
                        parentNode.Low = newNodeId;
                    }
                    else if (compRes > 0)
                    {
                        parentNode.High = newNodeId;
                    }
                    else
                    {
                        throw new ArgumentException("guid already exists");
                    }

                    // add node
                    var newNodeOffset = newNodeId * bstNodeSize;
                    var node = new BSTNode(guid, dataOffset, data.Length);
                    indexAccessor.Write(newNodeOffset, ref node);
                }

                // update parent
                indexAccessor.Write(parentNodeOffset, ref parentNode);
                indexAccessor.Flush();
                flushMetadata.Wait();
            }

            flushData.Wait();
        }

        public void Update(Guid guid, byte[] data)
        {
            var insertDataResult = InsertData(data);
            var newDataOffset = insertDataResult.Item1;
            var flushDataTask = insertDataResult.Item2;

            UpdateIndex(guid, newDataOffset);
            flushDataTask.Wait();
        }

        private (long, Task) InsertData(byte[] data)
        {
            // update metadata
            var dataOffset = metadata.Data.NextOffset;
            metadata.Data.NextOffset += data.Length;
            metadataAccessor.Write(0, ref metadata);

            // save data
            var dataAccessor = dataFile.CreateViewAccessor(dataOffset, data.Length);
            dataAccessor.WriteArray(0, data, 0, data.Length);

            // async flush task
            var flushData = Task.Run(() =>
            {
                dataAccessor.Flush();
                dataAccessor.Dispose();
            });

            return (dataOffset, flushData);
        }

        public void Delete(Guid guid)
        {
            // DataOffset -1 = deleted
            UpdateIndex(guid, -1);
        }

        private void UpdateIndex(Guid guid, long dataOffset)
        {
            var closestMatch = ClosestNode(guid);
            var bstNode = closestMatch.Item1;
            var offset = closestMatch.Item2;

            if (!Equals(guid, bstNode.Guid))
            {
                throw new RecordNotFoundException("entry for guid not found");
            }

            bstNode.DataOffset = dataOffset;

            using (var indexAccessor = indexFile.CreateViewAccessor(bstPosition + offset, 0))
            {
                indexAccessor.Write(0, ref bstNode);
            }
        }

    }
}