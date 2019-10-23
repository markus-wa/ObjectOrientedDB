using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ObjectOrientedDB.FileStorage
{
    interface IIndex
    {
        IndexEntry Find(Guid guid);
        Task Insert(Guid guid, long dataOffset, long dataSize);
        void Update(Guid guid, long newDataOffset, long newDataLength);
        IndexEntry Delete(Guid guid);
    }

    class Index : IIndex, IDisposable
    {
        internal struct Metadata
        {
            public long NextBSTNode;
        }

        private readonly MemoryMappedFile file;
        private readonly MemoryMappedViewAccessor metadataAccessor;
        private readonly long bstPosition;
        private readonly long bstNodeSize;

        private Metadata metadata;

        public Index(MemoryMappedFile file)
        {
            this.file = file;

            var metadataSize = Marshal.SizeOf(typeof(Metadata));
            metadataAccessor = file.CreateViewAccessor(0, metadataSize);
            metadataAccessor.Read(0, out metadata);

            // BST
            bstPosition = metadataSize;
            bstNodeSize = Marshal.SizeOf(typeof(IndexEntry));
        }

        public void Dispose()
        {
            metadataAccessor.Dispose();
            file.Dispose();
        }

        public IndexEntry Find(Guid guid)
        {
            IndexEntry bstNode = ClosestNode(guid).Item1;

            if (!Equals(guid, bstNode.Guid))
            {
                throw new RecordNotFoundException("entry for guid not found");
            }

            if (bstNode.DataOffset < 0)
            {
                throw new RecordNotFoundException("entry deleted");
            }
            return bstNode;
        }

        // avg: O(log n)
        // worst: O(n)
        private (IndexEntry, long) ClosestNode(Guid guid)
        {
            IndexEntry bstNode;
            long bstNodeOffset = 0;

            using (var indexAccessor = file.CreateViewAccessor(bstPosition, 0))
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

        public async Task Insert(Guid guid, long dataOffset, long dataSize)
        {
            using (var indexAccessor = file.CreateViewAccessor(bstPosition, 0))
            {
                // update BST
                var closestMatch = ClosestNode(guid);
                var parentNode = closestMatch.Item1;
                var parentNodeOffset = closestMatch.Item2;

                Task flushMetadata;
                // TODO: not happy with this check
                if (metadata.NextBSTNode == 0)
                {
                    // needed to handle root node / first entry correctly
                    metadata.NextBSTNode = 1;
                    metadataAccessor.Write(0, ref metadata);
                    flushMetadata = Task.Run(() => metadataAccessor.Flush());

                    parentNode.Guid = guid;
                    parentNode.DataOffset = dataOffset;
                    parentNode.DataSize = dataSize;
                }
                else
                {
                    // normal path
                    long newNodeId = metadata.NextBSTNode;
                    metadata.NextBSTNode++;
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
                    var node = new IndexEntry(guid, dataOffset, dataSize);
                    indexAccessor.Write(newNodeOffset, ref node);
                }

                // update parent
                indexAccessor.Write(parentNodeOffset, ref parentNode);

                var flushIndex = Task.Run(() =>
                {
                    indexAccessor.Flush();
                });
                await Task.WhenAll(flushMetadata, flushIndex);
            }
        }

        public void Update(Guid guid, long newDataOffset, long newDataLength)
        {
            UpdateInternal(guid, newDataOffset, newDataLength);
        }

        public IndexEntry Delete(Guid guid)
        {
            // DataOffset -1 = deleted
            return UpdateInternal(guid, -1, -1);
        }

        private IndexEntry UpdateInternal(Guid guid, long newDataOffset, long newDataLength)
        {
            var closestMatch = ClosestNode(guid);
            var bstNode = closestMatch.Item1;
            var offset = closestMatch.Item2;

            if (!Equals(guid, bstNode.Guid))
            {
                throw new RecordNotFoundException("entry for guid not found");
            }

            bstNode.DataOffset = newDataOffset;

            // -1 means delete, don't update DataSize in that case
            if (newDataLength != -1)
            {
                bstNode.DataSize = newDataLength;
            }

            using (var indexAccessor = file.CreateViewAccessor(bstPosition + offset, 0))
            {
                indexAccessor.Write(0, ref bstNode);
            }

            return bstNode;
        }
    }
}
