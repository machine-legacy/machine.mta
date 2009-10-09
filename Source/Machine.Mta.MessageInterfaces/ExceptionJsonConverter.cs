using System;
using System.IO;

using Newtonsoft.Json;

namespace Machine.Mta.InterfacesAsMessages
{
  public class ExceptionJsonConverter : JsonConverter
  {
    public override bool CanConvert(Type objectType)
    {
      return typeof(Exception).IsAssignableFrom(objectType);
    }

    public override object ReadJson(JsonReader reader, Type objectType)
    {
      string base64 = reader.Value.ToString();
      using (MemoryStream stream = new MemoryStream(Convert.FromBase64String(base64)))
      {
        return Serializers.Binary.Deserialize(stream);
      }
    }

    public override void WriteJson(JsonWriter writer, object value)
    {
      using (MemoryStream stream = new MemoryStream())
      {
        Serializers.Binary.Serialize(stream, value);
        writer.WriteValue(Convert.ToBase64String(stream.ToArray()));
      }
    }
  }
}