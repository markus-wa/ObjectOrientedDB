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
        private readonly long indexPosition;
        private readonly long bstPosition;
        private readonly long dataPosition;
        private readonly long indexEntrySize;
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

            // index
            indexPosition = metadataSize;

            // BST
            indexEntrySize = Marshal.SizeOf(typeof(IndexEntry));
            var indexSize = metadata.Index.Size * indexEntrySize;
            bstPosition = indexPosition + indexSize;

            // data
            bstNodeSize = Marshal.SizeOf(typeof(BSTNode));
            var bstSize = metadata.Index.Size * bstNodeSize * 2;
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

            // add index entry
            var indexEntryOffset = indexPosition + indexId * indexEntrySize;
            var guid = guidProvider();
            using (var indexAccessor = file.CreateViewAccessor(indexEntryOffset, indexEntrySize))
            {
                var indexEntry = new IndexEntry(guid, dataOffset, data.Length);
                indexAccessor.Write(0, ref indexEntry);
            }

            // update BST
            var res = ClosestNode(guid);
            var bstNode = res.Item1;
            var bstNodeOffset = res.Item2;

            // TODO: not happy with this check
            if (metadata.Index.NextBSTNode == 0)
            {
                // needed to handle root node / first entry correctly
                metadata.Index.NextBSTNode = 1;
                metadataAccessor.Write(0, ref metadata);

                bstNode.Low = indexId;
                bstNode.High = indexId;
            }
            else
            {
                var lowerNodeId = metadata.Index.NextBSTNode++;
                var higherNodeId = metadata.Index.NextBSTNode++;

                // update metadata
                metadataAccessor.Write(0, ref metadata);

                var lowerNodeOffset = lowerNodeId * bstNodeSize;
                using (var bstAccessor = file.CreateViewAccessor(bstPosition + lowerNodeOffset, 2 * bstNodeSize))
                {
                    var isNewLower = guid.CompareTo(GuidForIndex(bstNode.Low)) < 0;

                    // add lower node
                    long low = isNewLower ? indexId : bstNode.Low;
                    var lowerNode = new BSTNode(low, low);
                    bstAccessor.Write(0, ref lowerNode);

                    // add higher node
                    var high = !isNewLower ? indexId : bstNode.High;
                    var higherNode = new BSTNode(high, high);
                    bstAccessor.Write(bstNodeSize, ref higherNode);
                }

                // split up found node
                bstNode.Low = lowerNodeId;
                bstNode.High = higherNodeId;
            }

            using (var bstAccessor = file.CreateViewAccessor(bstPosition + bstNodeOffset, bstNodeSize))
            {
                bstAccessor.Write(0, ref bstNode);
            }

            return guid;
        }

        private Guid GuidForIndex(long indexId)
        {
            var indexEntryOffset = indexId * indexEntrySize;
            IndexEntry indexEntry;
            using (var indexAccessor = file.CreateViewAccessor(indexPosition + indexEntryOffset, indexEntrySize))
            {
                indexAccessor.Read(0, out indexEntry);
            }
            return indexEntry.Guid();
        }

        public byte[] Read(Guid guid)
        {
            throw new NotImplementedException();
        }

        private Tuple<BSTNode, long> ClosestNode(Guid guid)
        {
            BSTNode bstNode;
            using (var bstAccessor = file.CreateViewAccessor(bstPosition, bstNodeSize))
            {
                // read root node
                bstAccessor.Read(0, out bstNode);
            }

            var guidBytes = guid.ToByteArray();
            int guidBitPos = 0;
            long bstNodeOffset;
            do
            {
                // https://stackoverflow.com/a/15315676/4463023
                int guidByteIndex = guidBitPos / 8;
                int guidBitOffset = guidBitPos % 8;
                bool isGuidBitSet = (guidBytes[guidByteIndex] & (1 << guidBitOffset)) != 0;
                guidBitPos++;

                var nextNodeId = isGuidBitSet ? bstNode.High : bstNode.Low;
                bstNodeOffset = nextNodeId * bstNodeSize;

                using (var bstAccessor = file.CreateViewAccessor(bstPosition + bstNodeOffset, bstNodeSize))
                {
                    bstAccessor.Read(0, out bstNode);
                }
            } while (bstNode.Low != bstNode.High);

            return Tuple.Create(bstNode, bstNodeOffset);
        }
    }
}