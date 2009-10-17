using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Machine.Mta
{
  public class MessagePayloadConverter : TypeConverter
  {
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
      return typeof(String).IsAssignableFrom(sourceType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
    {
      return MessagePayload.FromString(value.ToString());
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
      return typeof(String).IsAssignableFrom(destinationType);
    }

    public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
    {
      MessagePayload payload = (MessagePayload)value;
      return payload.MakeString();
    }
  }
  [Serializable]
  [TypeConverter(typeof(MessagePayloadConverter))]
  public class MessagePayload
  {
    readonly byte[] _data;

    public byte[] ToByteArray()
    {
      return _data;
    }

    public MessagePayload(byte[] data)
    {
      _data = data;
    }

    public string MakeString()
    {
      return Convert.ToBase64String(ToByteArray());
    }

    public static MessagePayload FromString(string value)
    {
      return new MessagePayload(Convert.FromBase64String(value));
    }
  }
}
