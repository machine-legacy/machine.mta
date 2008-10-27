using System;
using System.Collections.Generic;

using MassTransit.ServiceBus;

namespace Machine.Mta.Minimalistic
{
  public class ProvideHandlerOrderInvoker<T> : IProvideHandlerOrderFor<IMessage>  where T : class, IMessage
  {
    private readonly IProvideHandlerOrderFor<T> _target;

    public ProvideHandlerOrderInvoker(IProvideHandlerOrderFor<T> target)
    {
      _target = target;
    }

    public IEnumerable<Type> GetHandlerOrder()
    {
      return _target.GetHandlerOrder();
    }

    public override string ToString()
    {
      return _target.ToString();
    }
  }

  public class HandlerInvoker<T> : Consumes<IMessage>.All where T : class, IMessage
  {
    private readonly Consumes<T>.All _target;

    public HandlerInvoker(Consumes<T>.All target)
    {
      _target = target;
    }

    public void Consume(IMessage message)
    {
      _target.Consume((T)message);
    }

    public override string ToString()
    {
      return _target.ToString();
    }
  }
  
  public static class Invokers
  {
    public static Consumes<IMessage>.All CreateForHandler(Type messageType, object handler)
    {
      return (Consumes<IMessage>.All)Activator.CreateInstance(typeof(HandlerInvoker<>).MakeGenericType(messageType), handler);
    }

    public static IProvideHandlerOrderFor<IMessage> CreateForHandlerOrderProvider(Type messageType, object handler)
    {
      return (IProvideHandlerOrderFor<IMessage>)Activator.CreateInstance(typeof(ProvideHandlerOrderInvoker<>).MakeGenericType(messageType), handler);
    }
  }
}