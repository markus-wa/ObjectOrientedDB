namespace ObjectOrientedDB
{
    public interface Serializer<T>
    {
        byte[] Serialize(T obj);

        D Deserialize<D>(byte[] data) where D : T;
    }
}