using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ObjectOrientedDB
{
    public class BinaryFormatterSerializer : Serializer<object>
    {

        private readonly BinaryFormatter formatter = new BinaryFormatter();

        public D Deserialize<D>(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                return (D)formatter.Deserialize(ms);
            }
        }

        public byte[] Serialize(object obj)
        {
            using (var ms = new MemoryStream())
            {
                formatter.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

    }
}