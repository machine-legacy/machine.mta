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
      return new MessagePayload(Convert.FromBase64String(value.ToString()));
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
      return typeof(String).IsAssignableFrom(destinationType);
    }

    public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
    {
      MessagePayload payload = (MessagePayload)value;
      return Convert.ToBase64String(payload.ToByteArray());
    }
  }
  [Serializable]
  [TypeConverter(typeof(MessagePayloadConverter))]
  public class MessagePayload
  {
    private readonly byte[] _data;

    public byte[] ToByteArray()
    {
      return _data;
    }

    public MessagePayload(byte[] data)
    {
      _data = data;
    }
  }
}
