using System;
using System.Collections.Generic;
using System.IO;
using Machine.Mta.InterfacesAsMessages;

namespace Machine.Mta.Specs
{
  public class InterfaceFormatterSpecs
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
      MessageRegisterer registerer = new MessageRegisterer();
      registerer.AddMessageTypes(typeof(IUserCreated), typeof(IHasChildren));
      MessageInterfaceImplementations messageInterfaceImplementor = new MessageInterfaceImplementations(new DefaultMessageInterfaceImplementationFactory(), registerer);
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
      MessageRegisterer registerer = new MessageRegisterer();
      registerer.AddMessageTypes(typeof(IMessage));
      MessageInterfaceImplementations implementations = new MessageInterfaceImplementations(new DefaultMessageInterfaceImplementationFactory(), registerer);
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
