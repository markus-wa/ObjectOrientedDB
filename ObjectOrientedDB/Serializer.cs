namespace ObjectOrientedDB
{
    /// <summary>
    /// Interface for serialization and deserialization of objects from/to byte arrays.
    /// </summary>
    /// <typeparam name="T">Type of objects the serializer can serialize/deserialize</typeparam>
    public interface Serializer<T>
    {
        /// <summary>
        /// Serializes <paramref name="obj"/> to a byte array.
        /// </summary>
        /// <param name="obj">The object to serialize</param>
        /// <returns>The serialized object</returns>
        byte[] Serialize(T obj);

        /// <summary>
        /// Deserializes an object from a byte array to <typeparamref name="D"/>.
        /// </summary>
        /// <typeparam name="D">The object type to deserialize to</typeparam>
        /// <param name="data">The data to deserialize</param>
        /// <returns>Deserialized object</returns>
        D Deserialize<D>(byte[] data) where D : T;
    }
}