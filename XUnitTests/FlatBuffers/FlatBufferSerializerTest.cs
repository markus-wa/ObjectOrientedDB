using FlatBuffers;
using ObjectOrientedDB.FlatBuffers;
using System;
using System.Collections.Generic;
using Testdata;
using Xunit;

namespace XUnitTests
{
    public class FlatBufferSerializerTest
    {

        [Fact]
        public void SerializeDeserialize()
        {
            var factories = new List<Func<ByteBuffer, IFlatbufferObject>> { (data) => Data_8b.GetRootAsData_8b(data) };
            var serializer = new FlatBufferSerializer(factories);

            Data_8b original;
            {
                var builder = new FlatBufferBuilder(8);
                var offset = Data_8b.CreateData_8b(builder);
                builder.Finish(offset.Value);
                original = Data_8b.GetRootAsData_8b(builder.DataBuffer);
            }

            var b = serializer.Serialize(original);
            var deserialized = serializer.Deserialize<Data_8b>(b);

            Assert.Equal(original.Val1, deserialized.Val1);
            Assert.Equal(original.Val2, deserialized.Val2);
        }

    }
}
