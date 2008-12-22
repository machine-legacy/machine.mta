using System;
using System.Collections.Generic;

using Machine.Mta.Sagas;

namespace Machine.Mta.Internal
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

  public class HandlerInvoker<T> : IConsume<IMessage> where T : class, IMessage
  {
    private readonly IConsume<T> _target;

    public HandlerInvoker(IConsume<T> target)
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

  public class RepositoryInvoker<T> : ISagaStateRepository<ISagaState> where T : class, ISagaState
  {
    readonly ISagaStateRepository<T> _target;

    public RepositoryInvoker(ISagaStateRepository<T> target)
    {
      _target = target;
    }

    public ISagaState FindSagaState(Guid sagaId)
    {
      return _target.FindSagaState(sagaId);
    }

    public void Save(ISagaState sagaState)
    {
      _target.Save((T)sagaState);
    }

    public void Delete(ISagaState sagaState)
    {
      _target.Delete((T)sagaState);
    }
  }
  
  public static class Invokers
  {
    public static IConsume<IMessage> CreateForHandler(Type messageType, object target)
    {
      return (IConsume<IMessage>)Activator.CreateInstance(typeof(HandlerInvoker<>).MakeGenericType(messageType), target);
    }

    public static IProvideHandlerOrderFor<IMessage> CreateForHandlerOrderProvider(Type messageType, object target)
    {
      return (IProvideHandlerOrderFor<IMessage>)Activator.CreateInstance(typeof(ProvideHandlerOrderInvoker<>).MakeGenericType(messageType), target);
    }

    public static ISagaStateRepository<ISagaState> CreateForRepository(Type sagaStateType, object target)
    {
      return (ISagaStateRepository<ISagaState>)Activator.CreateInstance(typeof(RepositoryInvoker<>).MakeGenericType(sagaStateType), target);
    }
  }
}