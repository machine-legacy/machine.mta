using System;
using System.Collections.Generic;
using System.Text;

namespace Machine.Mta.MessageInterfaces
{
  public class OpaqueMessageFactory
  {
    readonly MessageInterfaceImplementations _messageInterfaceImplementor;
    readonly MessageDefinitionFactory _messageDefinitionFactory;

    public OpaqueMessageFactory(MessageInterfaceImplementations messageInterfaceImplementor, MessageDefinitionFactory messageDefinitionFactory)
    {
      _messageInterfaceImplementor = messageInterfaceImplementor;
      _messageDefinitionFactory = messageDefinitionFactory;
    }

    public void Initialize(IEnumerable<Type> types)
    {
      _messageInterfaceImplementor.Initialize(types);
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
      var dictionary = value as IDictionary<string, object> ?? value.ToDictionary();
      CheckForErrors(typeof(T), dictionary);
      return (T)Create(typeof(T), dictionary);
    }

    public object Create<T>(Action<T> factory)
    {
      var message = Create(typeof(T));
      factory((T)message);
      return message;
    }

    void CheckForErrors(Type messageType, IDictionary<string, object> dictionary)
    {
      var sb = new StringBuilder();
      var definition = _messageDefinitionFactory.CreateDefinition(messageType);
      foreach (var error in definition.VerifyDictionaryAndReturnMissingProperties(dictionary))
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