using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Machine.Mta.AdoNet
{
  public class BinarySagaSerializer
  {
    public byte[] Serialize(object state)
    {
      using (MemoryStream stream = new MemoryStream())
      {
        Serializers.Binary.Serialize(stream, state);
        return stream.ToArray();
      }
    }
    
    public T Deserialize<T>(byte[] bytes)
    {
      using (MemoryStream stream = new MemoryStream(bytes))
      {
        return (T)Serializers.Binary.Deserialize(stream);
      }
    }
  }
}