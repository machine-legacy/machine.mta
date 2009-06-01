using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Machine.Mta
{
  public class EndpointAddressJsonConverter : JsonConverter
  {
    public override bool CanConvert(Type objectType)
    {
      return typeof(EndpointAddress).IsAssignableFrom(objectType);
    }

    public override object ReadJson(JsonReader reader, Type objectType)
    {
      return EndpointAddress.FromString(reader.Value.ToString());
    }

    public override void WriteJson(JsonWriter writer, object value)
    {
      writer.WriteValue(value.ToString());
    }
  }
}
