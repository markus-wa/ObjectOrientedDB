using System;
using Xunit;

namespace ObjectOrientedDB.FileStorage
{
    public class TreeNodeTest
    {
        [Fact]
        public void Test()
        {
            var guid = Guid.NewGuid();

            var node = new BSTNode(guid, 1, 2);

            Assert.Equal(guid, node.Guid);
            Assert.Equal(1, node.DataOffset);
            Assert.Equal(2, node.DataSize);
        }
    }
}
