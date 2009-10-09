using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Newtonsoft.Json;

namespace Machine.Mta.InterfacesAsMessages
{
  public class MessageInterfaceTransportFormatter : ITransportMessageBodyFormatter
  {
    private readonly MessageInterfaceImplementations _messageInterfaceImplementor;

    public MessageInterfaceTransportFormatter(MessageInterfaceImplementations messageInterfaceImplementor)
    {
      _messageInterfaceImplementor = messageInterfaceImplementor;
    }

    public void Serialize(IMessage[] messages, Stream stream)
    {
      using (StreamWriter writer = new StreamWriter(stream))
      {
        JsonSerializer serializer = Serializers.NewJson();
        serializer.Converters.Add(new InterfaceMessageJsonConverter(_messageInterfaceImplementor, false, serializer));
        serializer.Serialize(writer, messages);
      }
    }

    public IMessage[] Deserialize(Stream stream)
    {
      using (StreamReader reader = new StreamReader(stream))
      {
        JsonSerializer serializer = Serializers.NewJson();
        serializer.Converters.Add(new InterfaceMessageJsonConverter(_messageInterfaceImplementor, true, serializer));
        return (IMessage[])serializer.Deserialize(new JsonTextReader(reader), typeof(IMessage[]));
      }
    }
  }

  public class InterfaceMessageJsonConverter : JsonConverter
  {
    private static readonly string MessageTypePropertyName = "MessageType";
    private static readonly string MessageBodyPropertyName = "MessageBody";
    private readonly MessageInterfaceImplementations _messageInterfaceImplementor;
    private readonly bool _ignoreClassesBecauseWeAreReading;
    private readonly JsonSerializer _serializer;
    private bool _skipNext;

    public InterfaceMessageJsonConverter(MessageInterfaceImplementations messageInterfaceImplementor, bool reading, JsonSerializer serializer)
    {
      _messageInterfaceImplementor = messageInterfaceImplementor;
      _serializer = serializer;
      _ignoreClassesBecauseWeAreReading = reading;
    }

    public override bool CanConvert(Type objectType)
    {
      if (_skipNext)
      {
        return _skipNext = false;
      }
      if (typeof(IMessage).IsAssignableFrom(objectType))
      {
        return !_ignoreClassesBecauseWeAreReading || objectType.IsInterface;
      }
      return false;
    }

    public override object ReadJson(JsonReader reader, Type objectType)
    {
      if (reader.TokenType == JsonToken.Null)
      {
        return null;
      }
      reader.Read();
      System.Diagnostics.Debug.Assert(reader.Value.Equals(MessageTypePropertyName));
      
      reader.Read();
      string messageTypeName = reader.Value.ToString();
      
      reader.Read();
      System.Diagnostics.Debug.Assert(reader.Value.Equals(MessageBodyPropertyName));
     
      // Allow interfaces and non-interfaces...
      Type deserializeAs = MessageInterfaceHelpers.FindTypeNamed(messageTypeName, true);
      if (deserializeAs.IsInterface)
      {
        deserializeAs = _messageInterfaceImplementor.GetClassFor(deserializeAs);
      }
      object value = _serializer.Deserialize(reader, deserializeAs);
      reader.Read();
      return value;
    }

    public override void WriteJson(JsonWriter writer, object value)
    {
      Type objectType = value.GetType();
      Type messageType = objectType;
      // Allow interfaces and non-interfaces...
      if (_messageInterfaceImplementor.IsClassOrInterface(objectType))
      {
        messageType = _messageInterfaceImplementor.GetInterfaceFor(value.GetType());
      }
      writer.WriteStartObject();
      writer.WritePropertyName(MessageTypePropertyName);
      writer.WriteValue(messageType.FullName);
      writer.WritePropertyName(MessageBodyPropertyName);
      _skipNext = true;
      _serializer.Serialize(writer, value);
      writer.WriteEndObject();
    }
  }

  public static class MessageInterfaceHelpers
  {
    public static Type FindTypeNamed(string name, bool throwOnNull)
    {
      Type deserializeAs = Type.GetType(name);
      if (deserializeAs != null)
      {
        return deserializeAs;
      }
      foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
        deserializeAs = assembly.GetType(name);
        if (deserializeAs != null && typeof(IMessage).IsAssignableFrom(deserializeAs))
        {
          return deserializeAs;
        }
      }
      if (throwOnNull)
      {
        throw new InvalidOperationException("Unable to find " + name);
      }
      return null;
    }
  }
}