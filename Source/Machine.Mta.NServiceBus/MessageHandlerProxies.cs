using System;
using System.Collections.Generic;
using NServiceBus;

namespace Machine.Mta
{
  public class MessageHandlerProxy<T, K> : IMessageHandler<T> where T : class, NServiceBus.IMessage where K: Machine.Mta.IConsume<T>
  {
    readonly log4net.ILog _log;
    readonly INsbMessageBusFactory _messageBusFactory;
    readonly K _target;

    public MessageHandlerProxy(K target, INsbMessageBusFactory messageBusFactory)
    {
      _target = target;
      _messageBusFactory = messageBusFactory;
      _log = log4net.LogManager.GetLogger(typeof(K));
    }

    public void Handle(T message)
    {
      var bus = _messageBusFactory.CurrentBus().Bus;
      var nsbContext = bus.CurrentMessageContext;
      using (CurrentMessageContext.Open(nsbContext.ReturnAddress.ToEndpointAddress(), nsbContext.Id))
      {
        _target.Consume(message);
      }
    }

    public override string ToString()
    {
      return _target.ToString();
    }
  }

  public static class MessageHandlerProxies
  {
    public static Type For(Type messageType, Type handlerType)
    {
      return typeof(MessageHandlerProxy<,>).MakeGenericType(messageType, handlerType);
    }

    public static EndpointAddress ToEndpointAddress(this string value)
    {
      return EndpointAddress.FromString(value);
    }
  }
}
