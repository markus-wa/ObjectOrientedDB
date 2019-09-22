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

        public FileStorageEngine(MemoryMappedFile file, GuidProvider p = null)
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
                metadata.Index.Size = DEFAULT_INDEX_SIZE;
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
            var indexId = metadata.Index.NextEntry++;
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
            var bstNode = closestMatch.Item1;
            var bstNodeOffset = closestMatch.Item2;

            // TODO: not happy with this check
            if (metadata.Index.NextBSTNode == 0)
            {
                // needed to handle root node / first entry correctly
                metadata.Index.NextBSTNode = 1;
                metadataAccessor.Write(0, ref metadata);

                bstNode.Guid = guid;
                bstNode.DataOffset = dataOffset;
                bstNode.Size = data.Length;
            }
            else
            {
                var newNodeId = metadata.Index.NextBSTNode;

                var compRes = guid.CompareTo(bstNode.Guid);
                if (compRes < 0)
                {
                    bstNode.Low = newNodeId;
                }
                else if (compRes > 0)
                {
                    bstNode.High = newNodeId;
                }
                else
                {
                    throw new ArgumentException("guid already exists");
                }

                metadata.Index.NextBSTNode++;
                metadataAccessor.Write(0, ref metadata);

                var newNodeOffset = newNodeId * bstNodeSize;
                using (var bstAccessor = GetBSTAccessor(bstPosition + newNodeOffset, bstNodeSize))
                {
                    // add node
                    var node = new BSTNode(guid, dataOffset, data.Length);
                    bstAccessor.Write(0, ref node);
                }
            }

            using (var bstAccessor = GetBSTAccessor(bstPosition + bstNodeOffset, bstNodeSize))
            {
                bstAccessor.Write(0, ref bstNode);
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
            using (var bstAccessor = GetBSTAccessor(bstPosition, bstNodeSize))
            {
                // read root node
                bstAccessor.Read(0, out bstNode);
            }

            long bstNodeOffset = 0;
            while (true)
            {
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

                using (var bstAccessor = GetBSTAccessor(bstPosition + bstNodeOffset, bstNodeSize))
                {
                    bstAccessor.Read(0, out bstNode);
                }
            }
        }
    }
}