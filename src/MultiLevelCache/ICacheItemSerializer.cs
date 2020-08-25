namespace MultiLevelCaching
{
    public interface ICacheItemSerializer
    {
        T Deserialize<T>(byte[] valueBytes);
        byte[] Serialize<T>(T value);
    }
}
