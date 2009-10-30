using System;
using System.Collections.Generic;
using Machine.Mta.MessageInterfaces;

namespace Machine.Mta
{
  public interface IMessageFactory
  {
    IMessage Create(Type type, params object[] parameters);
    T Create<T>() where T : IMessage;
    T Create<T>(object value) where T : IMessage;
    T Create<T>(Action<T> factory) where T : IMessage;
  }

  public class MessageFactory : IMessageFactory
  {
    readonly OpaqueMessageFactory _opaqueMessageFactory;

    public MessageFactory()
    {
      var messageInterfaceImplementor = new MessageInterfaceImplementations(new DefaultMessageInterfaceImplementationFactory());
      _opaqueMessageFactory = new OpaqueMessageFactory(messageInterfaceImplementor, new MessageDefinitionFactory());
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

    public void Initialize(IEnumerable<Type> types)
    {
      _opaqueMessageFactory.Initialize(types);
    }
  }
}
