using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Machine.Mta.AdoNet
{
  public class BinarySagaSerializer
  {
    readonly BinaryFormatter _formatter = new BinaryFormatter();
    
    public byte[] Serialize(object state)
    {
      using (MemoryStream stream = new MemoryStream())
      {
        _formatter.Serialize(stream, state);
        return stream.ToArray();
      }
    }
    
    public T Deserialize<T>(byte[] bytes)
    {
      using (MemoryStream stream = new MemoryStream(bytes))
      {
        return (T)_formatter.Deserialize(stream);
      }
    }
  }
}