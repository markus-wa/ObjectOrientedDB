using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

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

        public FileStorageEngine(MemoryMappedFile indexFile, MemoryMappedFile dataFile, long indexSize = DEFAULT_INDEX_SIZE)
        {
            this.indexFile = indexFile;
            this.dataFile = dataFile;

            // read metadata
            var metadataSize = Marshal.SizeOf(typeof(Metadata));
            metadataAccessor = indexFile.CreateViewAccessor(0, metadataSize);
            metadataAccessor.Read(0, out metadata);

            // init metadata if non existent
            if (metadata.Index.Size == 0)
            {
                metadata.Index.Size = indexSize;
                metadataAccessor.Write(0, ref metadata);
            }

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

        public static StorageEngine Create(string path, long size)
        {
            Directory.CreateDirectory(path);
            var indexFile = CreateFile(path + "/index", size);
            var dataFile = CreateFile(path + "/data", size);
            return new FileStorageEngine(indexFile, dataFile);
        }

        private static MemoryMappedFile CreateFile(string path, long size)
        {
            return MemoryMappedFile.CreateFromFile(path, FileMode.Create, path, size);
        }

        public static StorageEngine Open(string path)
        {
            var indexFile = OpenFile(path + "/index");
            var dataFile = OpenFile(path + "/data");
            return new FileStorageEngine(indexFile, dataFile);
        }

        private static MemoryMappedFile OpenFile(string path)
        {
            return MemoryMappedFile.CreateFromFile(path, FileMode.Open, path);
        }

        private MemoryMappedViewAccessor BSTAccessor(long position, long length)
        {
            return indexFile.CreateViewAccessor(bstPosition + position, length);
        }

        private MemoryMappedViewAccessor DataAccessor(long position, long length)
        {
            return dataFile.CreateViewAccessor(position, length);
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
            using (var dataAccessor = DataAccessor(bstNode.DataOffset, bstNode.Size))
            {
                var data = new byte[bstNode.Size];
                dataAccessor.ReadArray(0, data, 0, data.Length);
                return data;
            }
        }

        private (BSTNode, long) ClosestNode(Guid guid)
        {
            BSTNode bstNode;
            long bstNodeOffset = 0;

            while (true)
            {
                using (var bstAccessor = BSTAccessor(bstNodeOffset, bstNodeSize))
                {
                    bstAccessor.Read(0, out bstNode);
                }

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

        public void Insert(Guid guid, byte[] data)
        {
            // update metadata
            var dataOffset = metadata.Data.NextOffset;
            metadata.Data.NextOffset += data.Length;
            metadataAccessor.Write(0, ref metadata);

            // save data
            using (var dataAccessor = DataAccessor(dataOffset, data.Length))
            {
                dataAccessor.WriteArray(0, data, 0, data.Length);
            }

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

                var newNodeOffset = newNodeId * bstNodeSize;
                using (var bstAccessor = BSTAccessor(newNodeOffset, bstNodeSize))
                {
                    // add node
                    var node = new BSTNode(guid, dataOffset, data.Length);
                    bstAccessor.Write(0, ref node);
                }
            }

            // update parent
            using (var bstAccessor = BSTAccessor(parentNodeOffset, bstNodeSize))
            {
                bstAccessor.Write(0, ref parentNode);
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