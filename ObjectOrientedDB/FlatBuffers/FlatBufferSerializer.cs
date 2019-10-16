using System;
using System.Collections.Generic;
using FlatBuffers;
using ObjectOrientedDB;

namespace ObjectOrientedDB.FlatBuffers
{
    public class FlatBufferSerializer : Serializer<IFlatbufferObject>
    {

        private readonly Dictionary<Type, Func<ByteBuffer, IFlatbufferObject>> ObjectFactories = new Dictionary<Type, Func<ByteBuffer, IFlatbufferObject>>();

        public FlatBufferSerializer(List<Func<ByteBuffer, IFlatbufferObject>> ObjectFactories)
        {
            var bb = new ByteBuffer(new byte[] { 0, 0, 0, 0 });
            foreach (Func<ByteBuffer, IFlatbufferObject> item in ObjectFactories)
            {
                var type = item.Invoke(bb).GetType();
                this.ObjectFactories.Add(type, item);
            }
        }

        public D Deserialize<D>(byte[] data) where D : IFlatbufferObject
        {
            var factory = ObjectFactories[typeof(D)];
            return (D)factory.Invoke(new ByteBuffer(data));
        }

        public byte[] Serialize(IFlatbufferObject obj)
        {
            return obj.ByteBuffer.ToSizedArray();
        }

    }
}