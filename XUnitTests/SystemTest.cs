using ObjectOrientedDB;
using ObjectOrientedDB.FileStorage;
using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace XUnitTests
{
    public class SystemTest : IDisposable
    {
        public readonly long SIZE_1G = 1L * 1024 * 1024 * 1024;

        public SystemTest()
        {
            Cleanup();
        }

        public void Dispose()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            if (Directory.Exists("dbs"))
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Directory.Delete("dbs", true);
            }
        }

        [Fact]
        public void T1_DBCreate()
        {
            using (var db = NewDB("t1"))
            {
                Assert.NotNull(db);
            }

            Assert.True(File.Exists("dbs/t1/index"));
            Assert.True(File.Exists("dbs/t1/data"));
        }

        [Fact]
        public void T2_DBOpen()
        {
            Assert.False(Directory.Exists("dbs/t2"));
            using (NewDB("t2"))
            {
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();

            using (var db = OpenDB("t2"))
            {
                Assert.NotNull(db);
            }
        }

        [Fact]
        public void T3_T4_Insert_Read()
        {
            var original = new Testdata(1);
            Guid guid;
            using (var db = NewDB("t3_t4"))
            {
                guid = db.Insert(original);
            }

            using (var db = OpenDB("t3_t4"))
            {
                Testdata read = db.Read<Testdata>(guid);

                original.Should().BeEquivalentTo(read);
            }
        }

        [Fact]
        public void T5_Update()
        {
            var original = new Testdata(1);
            Guid guid;
            using (var db = NewDB("t5"))
            {
                guid = db.Insert(original);

                original.Value = 2;
                db.Update(guid, original);
            }

            using (var db = OpenDB("t5"))
            {
                Testdata read = db.Read<Testdata>(guid);

                original.Should().BeEquivalentTo(read);
            }
        }

        [Fact]
        public void T6_Delete()
        {
            using (var db = NewDB("t6"))
            {
                var guid = db.Insert(new Testdata(1));
                db.Delete(guid);

                var ex = Assert.Throws<RecordNotFoundException>(() => db.Read<Testdata>(guid));
                ex.Message.Should().BeEquivalentTo("entry deleted");
            }
        }

        private ObjectDB<object> NewDB(string name)
        {
            return new ObjectDB<object>(FileStorageEngine.Create("dbs/" + name, SIZE_1G, 64), new BinaryFormatterSerializer());
        }

        private ObjectDB<object> OpenDB(string name)
        {
            return new ObjectDB<object>(FileStorageEngine.Open("dbs/" + name), new BinaryFormatterSerializer());
        }

    }

}
