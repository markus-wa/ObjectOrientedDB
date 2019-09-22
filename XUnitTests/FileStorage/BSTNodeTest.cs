using Xunit;

namespace ObjectOrientedDB.FileStorage
{
    public class TreeNodeTest
    {
        [Fact]
        public void Test()
        {
            var node = new BSTNode(1, 2);

            Assert.Equal(1, node.Low);
            Assert.Equal(2, node.High);
        }
    }
}
