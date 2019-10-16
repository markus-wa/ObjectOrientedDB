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
                throw new ArgumentException("entry for guid not found");
            }

            // read data
            var data = new byte[bstNode.Size];
            using (var dataAccessor = dataFile.CreateViewAccessor(bstNode.DataOffset, data.Length))
            {
                dataAccessor.ReadArray(bstNode.DataOffset, data, 0, data.Length);
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
                    long nextNodeId;
                    if (compRes < 0)
                    {
                        if (bstNode.Low == 0)
                        {
                            return (bstNode, bstNodeOffset);
                        }

                        nextNodeId = bstNode.Low;
                    }
                    else if (compRes > 0)
                    {
                        if (bstNode.High == 0)
                        {
                            return (bstNode, bstNodeOffset);
                        }

                        nextNodeId = bstNode.High;
                    }
                    else
                    {
                        return (bstNode, bstNodeOffset);
                    }

                    bstNodeOffset = nextNodeId * bstNodeSize;
                }
            }
        }

        public void Insert(Guid guid, byte[] data)
        {
            // update metadata
            var dataOffset = metadata.Data.NextOffset;
            metadata.Data.NextOffset += data.Length;
            metadataAccessor.Write(0, ref metadata);

            using (var dataAccessor = dataFile.CreateViewAccessor(dataOffset, data.Length))
            using (var indexAccessor = indexFile.CreateViewAccessor(bstPosition, 0))
            {
                // save data
                dataAccessor.WriteArray(0, data, 0, data.Length);
                var flush = Task.Run(() => dataAccessor.Flush());

                // update BST
                var closestMatch = ClosestNode(guid);
                var parentNode = closestMatch.Item1;
                var parentNodeOffset = closestMatch.Item2;

                // TODO: not happy with this check
                if (metadata.Index.NextBSTNode == 0)
                {
                    // needed to handle root node / first entry correctly
                    metadata.Index.NextBSTNode = 1;
                    metadataAccessor.Write(0, ref metadata);

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
                metadataAccessor.Flush();
                flush.Wait();
            }
        }

        public void Update(Guid guid, byte[] data)
        {
            throw new NotImplementedException();
        }

        public void Delete(Guid guid)
        {
            throw new NotImplementedException();
        }

    }
}