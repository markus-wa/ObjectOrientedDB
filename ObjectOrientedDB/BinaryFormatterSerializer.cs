using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ObjectOrientedDB
{
    /// <summary>
    /// Serializer implementation for objects with C#'s [Serializable] attribute.
    /// </summary>
    public class BinaryFormatterSerializer : Serializer<object>
    {

        private readonly BinaryFormatter formatter = new BinaryFormatter();

        /// <inheritdoc />
        public D Deserialize<D>(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                return (D)formatter.Deserialize(ms);
            }
        }

        /// <inheritdoc />
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