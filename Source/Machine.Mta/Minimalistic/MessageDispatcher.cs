using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Container.Model;
using Machine.Container.Services;

using MassTransit.ServiceBus;

namespace Machine.Mta.Minimalistic
{
  public class FutureHandlerInvocation
  {
    readonly Type _targetType;
    readonly Type _consumerType;

    public Type TargetType
    {
      get { return _targetType; }
    }

    public Type ConsumerType
    {
      get { return _consumerType; }
    }

    public Type TargetExpectsMessageOfType
    {
      get { return _consumerType.GetGenericArguments()[0]; }
    }

    public FutureHandlerInvocation(Type targetType, Type consumerType)
    {
      _targetType = targetType;
      _consumerType = consumerType;
    }

    public override string ToString()
    {
      return "Invoke " + this.TargetType.FullName + " to handle " + this.TargetExpectsMessageOfType.FullName;
    }
  }

  public class HandlerDiscoverer
  {
    private readonly IMachineContainer _container;

    public HandlerDiscoverer(IMachineContainer container)
    {
      _container = container;
    }

    public IEnumerable<FutureHandlerInvocation> GetHandlerInvocationsFor(Type messageType)
    {
      List<FutureHandlerInvocation> invocations = new List<FutureHandlerInvocation>();
      
      foreach (ServiceRegistration registration in _container.RegisteredServices)
      {
        Type handlerOfMessageType = typeof(Consumes<>.All).MakeGenericType(messageType);
        if (registration.ServiceType.IsSortOfContravariantWith(handlerOfMessageType))
        {
          foreach (Type interfaceType in registration.ServiceType.GetInterfaces())
          {
            if (interfaceType.IsSortOfContravariantWith(handlerOfMessageType))
            {
              invocations.Add(new FutureHandlerInvocation(registration.ServiceType, interfaceType));
            }
          }
        }
      }

      return invocations;
    }
  }

  public class MessageDispatcher
  {
    private readonly IMachineContainer _container;
    private readonly HandlerDiscoverer _handlerDiscoverer;

    public MessageDispatcher(IMachineContainer container)
    {
      _container = container;
      _handlerDiscoverer = new HandlerDiscoverer(container);
    }

    public void Dispatch(IMessage message)
    {
      foreach (FutureHandlerInvocation invocation in _handlerDiscoverer.GetHandlerInvocationsFor(message.GetType()))
      {
        object handler = _container.Resolve.Object(invocation.TargetType);
        Consumes<IMessage>.All invoker = Invokers.CreateForHandler(invocation.TargetExpectsMessageOfType, handler);
        invoker.Consume(message);
      }
    }
  }
}