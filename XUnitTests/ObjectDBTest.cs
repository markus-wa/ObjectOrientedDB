using Moq;
using ObjectOrientedDB;
using System;
using Xunit;

namespace XUnitTests
{
    public class ObjectDBTest
    {
        [Fact]
        public void InsertReturnsGuid()
        {
            var expectedGuid = Guid.NewGuid();

            var storageEngine = new Mock<StorageEngine>();
            Mock<Serializer<object>> serializer = new Mock<Serializer<object>>();
            ObjectDB<object> db = new ObjectDB<object>(storageEngine.Object, serializer.Object, () => expectedGuid);
            var guid = db.Insert(BitConverter.GetBytes(UInt64.MaxValue));

            Assert.Equal(expectedGuid, guid);
        }

    }

}
