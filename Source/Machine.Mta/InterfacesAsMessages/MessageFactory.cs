using System;
using System.Collections.Generic;
using System.Text;

namespace Machine.Mta.InterfacesAsMessages
{
  public class MessageFactory : IMessageFactory
  {
    readonly MessageInterfaceImplementations _messageInterfaceImplementor;
    readonly MessageDefinitionFactory _messageDefinitionFactory;

    public MessageFactory(MessageInterfaceImplementations messageInterfaceImplementor, MessageDefinitionFactory messageDefinitionFactory)
    {
      _messageInterfaceImplementor = messageInterfaceImplementor;
      _messageDefinitionFactory = messageDefinitionFactory;
    }

    public IMessage Create(Type type, params object[] parameters)
    {
      Type implementation = _messageInterfaceImplementor.GetClassFor(type);
      if (implementation == null || !type.IsAssignableFrom(implementation))
      {
        throw new InvalidOperationException();
      }
      return (IMessage)Activator.CreateInstance(implementation, parameters);
    }

    public T Create<T>() where T : IMessage
    {
      return (T)Create(typeof(T));
    }

    public T Create<T>(object value) where T : IMessage
    {
      IDictionary<string, object> dictionary = value as IDictionary<string, object> ?? value.ToDictionary();
      CheckForErrors(typeof(T), dictionary);
      return (T)Create(typeof(T), dictionary);
    }

    public T Create<T>(Action<T> factory) where T : IMessage
    {
      T message = Create<T>();
      factory(message);
      return message;
    }

    private void CheckForErrors(Type messageType, IDictionary<string, object> dictionary)
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
