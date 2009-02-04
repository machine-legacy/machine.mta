using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Newtonsoft.Json;

using Machine.Mta.Internal;

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

  public class InterfaceFormatterTests
  {
    public interface IUserCreated : IMessage
    {
      Guid UserId { get; set; }
      DateTime CreatedAt { get; set; }
    }

    public interface IHasChildren : IMessage
    {
      IUserCreated First { get; set; }
      IUserCreated Second { get; set; }
    }

    public void try_serialization_and_deserializing()
    {
      MessageInterfaceImplementations messageInterfaceImplementor = new MessageInterfaceImplementations(new DefaultMessageInterfaceImplementationFactory());
      messageInterfaceImplementor.AddMessageTypes(typeof(IUserCreated), typeof(IHasChildren));
      IMessageFactory factory = new MessageFactory(messageInterfaceImplementor, new MessageDefinitionFactory());
      MemoryStream stream = new MemoryStream();
      MessageInterfaceTransportFormatter formatter = new MessageInterfaceTransportFormatter(messageInterfaceImplementor);
      List<IMessage> messages = new List<IMessage>();
      IUserCreated created = factory.Create<IUserCreated>();
      created.UserId = Guid.NewGuid();
      created.CreatedAt = DateTime.UtcNow;
      messages.Add(created);
      IUserCreated first = factory.Create<IUserCreated>(new { UserId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow });
      IUserCreated second = factory.Create<IUserCreated>(new { UserId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow });
      IHasChildren hasChildren = factory.Create<IHasChildren>(new { First = first, Second = second });
      hasChildren.First = first;
      hasChildren.Second = second;
      messages.Add(hasChildren);
      formatter.Serialize(messages.ToArray(), stream);
      string text = System.Text.Encoding.Default.GetString(stream.ToArray());
      Console.WriteLine(text);
      IMessage[] received = formatter.Deserialize(new MemoryStream(stream.ToArray()));
      foreach (IMessage msg in received)
      {
        Console.WriteLine("Received: " + msg);
      }
    }

    public void binary_serialization_of_messages()
    {
      MessageInterfaceImplementations implementations = new MessageInterfaceImplementations(new DefaultMessageInterfaceImplementationFactory());
      implementations.AddMessageTypes(typeof(IMessage));
      MessageFactory factory = new MessageFactory(implementations, new MessageDefinitionFactory());
      byte[] buffer = null;
      using (MemoryStream stream = new MemoryStream())
      {
        Serializers.Binary.Serialize(stream, new SimpleObjectWithAMessage(factory.Create<IMessage>()));
        buffer = stream.ToArray();
      }
      using (MemoryStream stream = new MemoryStream(buffer))
      {
        object target = Serializers.Binary.Deserialize(stream);
      }
    }
  }
  [Serializable]
  public class SimpleObjectWithAMessage
  {
    readonly IMessage _message;

    public IMessage Message
    {
      get { return _message; }
    }

    public SimpleObjectWithAMessage(IMessage message)
    {
      _message = message;
    }
  }
}