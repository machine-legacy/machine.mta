using System;
using System.Collections.Generic;
using NServiceBus;

namespace Machine.Mta
{
  public class NsbMessageFactory : IMessageFactory
  {
    readonly INsbMessageBusFactory _messageBusFactory;

    public NsbMessageFactory(INsbMessageBusFactory messageBusFactory)
    {
      _messageBusFactory = messageBusFactory;
    }

    public IMessage Create(Type type, params object[] parameters)
    {
      if (parameters.Length > 0)
      {
        throw new NotSupportedException();
      }
      return (IMessage)_messageBusFactory.CurrentBus().Bus.CreateInstance(type);
    }

    public T Create<T>() where T : IMessage
    {
      return (T)_messageBusFactory.CurrentBus().Bus.CreateInstance(typeof(T));
    }

    public T Create<T>(object value) where T : IMessage
    {
      throw new NotImplementedException();
    }

    public T Create<T>(Action<T> factory) where T : IMessage
    {
      T message = Create<T>();
      factory(message);
      return message;
    }
  }
}
