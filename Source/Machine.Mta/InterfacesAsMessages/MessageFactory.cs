using System;
using System.Collections.Generic;
using System.Text;

namespace Machine.Mta.InterfacesAsMessages
{
  public class MessageFactory : IMessageFactory
  {
    readonly OpaqueMessageFactory _opaqueMessageFactory;

    public MessageFactory(MessageInterfaceImplementations messageInterfaceImplementor, MessageDefinitionFactory messageDefinitionFactory)
    {
      _opaqueMessageFactory = new OpaqueMessageFactory(messageInterfaceImplementor, messageDefinitionFactory);
    }

    public IMessage Create(Type type, params object[] parameters)
    {
      return (IMessage)_opaqueMessageFactory.Create(type, parameters);
    }

    public T Create<T>() where T : IMessage
    {
      return (T)_opaqueMessageFactory.Create(typeof(T));
    }

    public T Create<T>(object value) where T : IMessage
    {
      return (T)_opaqueMessageFactory.Create<T>(value);
    }

    public T Create<T>(Action<T> factory) where T : IMessage
    {
      return (T)_opaqueMessageFactory.Create<T>(factory);
    }
  }

  public class OpaqueMessageFactory
  {
    readonly MessageInterfaceImplementations _messageInterfaceImplementor;
    readonly MessageDefinitionFactory _messageDefinitionFactory;

    public OpaqueMessageFactory(MessageInterfaceImplementations messageInterfaceImplementor, MessageDefinitionFactory messageDefinitionFactory)
    {
      _messageInterfaceImplementor = messageInterfaceImplementor;
      _messageDefinitionFactory = messageDefinitionFactory;
    }

    public object Create(Type type, params object[] parameters)
    {
      if (type.IsClass)
      {
        return Activator.CreateInstance(type, parameters);
      }
      var implementation = _messageInterfaceImplementor.GetClassFor(type);
      if (implementation == null || !type.IsAssignableFrom(implementation))
      {
        throw new InvalidOperationException();
      }
      return Activator.CreateInstance(implementation, parameters);
    }

    public object Create<T>(object value)
    {
      IDictionary<string, object> dictionary = value as IDictionary<string, object> ?? value.ToDictionary();
      CheckForErrors(typeof(T), dictionary);
      return (T)Create(typeof(T), dictionary);
    }

    public object Create<T>(Action<T> factory)
    {
      object message = Create(typeof(T));
      factory((T)message);
      return message;
    }

    void CheckForErrors(Type messageType, IDictionary<string, object> dictionary)
    {
      StringBuilder sb = new StringBuilder();
      MessageDefinition definition = _messageDefinitionFactory.CreateDefinition(messageType);
      foreach (MessagePropertyError error in definition.VerifyDictionaryAndReturnMissingProperties(dictionary))
      {
        sb.AppendLine(error.Type + " " + messageType.Name + "." + error.Name);
      }
      if (sb.Length == 0)
      {
        return;
      }
      throw new MessageCreationException("Properties: \r\n" + sb);
    }
  }
}
