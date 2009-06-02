using System;
using System.Collections.Generic;
using NServiceBus;

namespace Machine.Mta
{
  public class MessageHandlerProxy<T, K> : IMessageHandler<T> where T : class, NServiceBus.IMessage, Machine.Mta.IMessage where K: Machine.Mta.IConsume<T>
  {
    readonly K _target;

    public MessageHandlerProxy(K target)
    {
      _target = target;
    }

    public void Handle(T message)
    {
      _target.Consume(message);
    }
  }

  public static class MessageHandlerProxies
  {
    public static Type For(Type messageType, Type handlerType)
    {
      return typeof(MessageHandlerProxy<,>).MakeGenericType(messageType, handlerType);
    }
  }
}
