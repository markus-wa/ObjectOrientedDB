using System;
using Xunit;

namespace ObjectOrientedDB.FileStorage
{
    public class IndexEntryTest
    {
        [Fact]
        public void Test()
        {
            var guid = Guid.NewGuid();

            var entry = new IndexEntry(guid, 1, 2);

            Assert.Equal(guid, entry.Guid());
            Assert.Equal(1, entry.DataOffset);
            Assert.Equal(2, entry.Size);
        }
    }
}
