using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace ObjectOrientedDB.FileStorage
{
    public class FileStorageEngine : StorageEngine
    {
        public delegate Guid GuidProvider();

        private readonly MemoryMappedFile file;
        private readonly GuidProvider guidProvider;
        private readonly MemoryMappedViewAccessor metadataAccessor;
        private readonly long bstPosition;
        private readonly long dataPosition;
        private readonly long bstNodeSize;
        private Metadata metadata;

        public const long DEFAULT_INDEX_SIZE = 64;

        public FileStorageEngine(MemoryMappedFile file, GuidProvider p = null, long indexSize = DEFAULT_INDEX_SIZE)
        {
            this.file = file;
            this.guidProvider = p ?? (() => Guid.NewGuid());

            // read metadata
            var metadataSize = Marshal.SizeOf(typeof(Metadata));
            metadataAccessor = file.CreateViewAccessor(0, metadataSize);
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
            var bstSize = metadata.Index.Size * bstNodeSize;
            dataPosition = bstPosition + bstSize;
        }

        public Guid Store(byte[] data)
        {
            // update metadata
            var dataOffset = metadata.Data.NextOffset;
            metadata.Data.NextOffset += data.Length;
            metadataAccessor.Write(0, ref metadata);

            // save data
            using (var dataAccessor = file.CreateViewAccessor(dataPosition + dataOffset, data.Length))
            {
                dataAccessor.WriteArray(0, data, 0, data.Length);
            }

            // update BST
            var guid = guidProvider();
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
                using (var bstAccessor = GetBSTAccessor(bstPosition + newNodeOffset, bstNodeSize))
                {
                    // add node
                    var node = new BSTNode(guid, dataOffset, data.Length);
                    bstAccessor.Write(0, ref node);
                }
            }

            // update parent
            using (var bstAccessor = GetBSTAccessor(bstPosition + parentNodeOffset, bstNodeSize))
            {
                bstAccessor.Write(0, ref parentNode);
            }

            return guid;
        }

        private MemoryMappedViewAccessor GetBSTAccessor(long position, long length)
        {
            if (position + length > dataPosition)
            {
                throw new ArgumentException("index full");
            }
            return file.CreateViewAccessor(position, length);
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
            using (var dataAccessor = file.CreateViewAccessor(dataPosition + bstNode.DataOffset, bstNode.Size))
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
                using (var bstAccessor = GetBSTAccessor(bstPosition + bstNodeOffset, bstNodeSize))
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
    }
}