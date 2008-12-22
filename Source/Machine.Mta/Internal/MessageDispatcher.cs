using System;
using System.Collections.Generic;
using System.Linq;

using MassTransit;

using Machine.Mta.Sagas;
using Machine.Container.Model;
using Machine.Container.Services;

namespace Machine.Mta.Internal
{
  public class MessageHandlerType
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

    public MessageHandlerType(Type targetType, Type consumerType)
    {
      _targetType = targetType;
      _consumerType = consumerType;
    }

    public override string ToString()
    {
      return "Invoke " + this.TargetType.FullName + " to handle " + this.TargetExpectsMessageOfType.FullName;
    }
  }

  public static class HandlerTypeHelpers
  {
    public static IEnumerable<Type> Interfaces(this Type type)
    {
      if (type.IsInterface)
      {
        yield return type;
      }
      foreach (Type interfaceType in type.GetInterfaces())
      {
        yield return interfaceType;
      }
    }
  }

  public class HandlerDiscoverer
  {
    private readonly IMachineContainer _container;

    public HandlerDiscoverer(IMachineContainer container)
    {
      _container = container;
    }

    public IEnumerable<MessageHandlerType> GetHandlerTypesFor(Type messageType)
    {
      List<MessageHandlerType> messageHandlerTypes = new List<MessageHandlerType>();
      
      foreach (ServiceRegistration registration in _container.RegisteredServices)
      {
        Type handlerOfMessageType = typeof(Consumes<>.All).MakeGenericType(messageType);
        if (registration.ServiceType.IsSortOfContravariantWith(handlerOfMessageType))
        {
          foreach (Type interfaceType in registration.ServiceType.Interfaces())
          {
            if (interfaceType.GetGenericArguments().Length > 0)
            {
              if (typeof(Consumes<>.All).MakeGenericType(interfaceType.GetGenericArguments()[0]).Equals(interfaceType))
              {
                if (interfaceType.IsSortOfContravariantWith(handlerOfMessageType))
                {
                  messageHandlerTypes.Add(new MessageHandlerType(registration.ServiceType, interfaceType));
                }
              }
            }
          }
        }
      }

      return ApplyOrdering(messageType, messageHandlerTypes);
    }

    private IEnumerable<MessageHandlerType> ApplyOrdering(Type messageType, IEnumerable<MessageHandlerType> handlerTypes)
    {
      List<MessageHandlerType> remaining = new List<MessageHandlerType>(handlerTypes);
      List<MessageHandlerType> ordered = new List<MessageHandlerType>();
      foreach (Type handlerOfType in GetHandlerOrderFor(messageType))
      {
        foreach (MessageHandlerType messageHandlerType in new List<MessageHandlerType>(remaining))
        {
          if (handlerOfType.IsAssignableFrom(messageHandlerType.TargetType))
          {
            ordered.Add(messageHandlerType);
            remaining.Remove(messageHandlerType);
            break;
          }
        }
      }
      ordered.AddRange(remaining);
      return ordered;
    }

    private IEnumerable<Type> GetHandlerOrderFor(Type messageType)
    {
      object orderer = _container.Resolve.All(type => {
        return type.IsSortOfContravariantWith(typeof(IProvideHandlerOrderFor<>).MakeGenericType(messageType));
      }).FirstOrDefault();
      if (orderer == null)
      {
        return new List<Type>();
      }
      IProvideHandlerOrderFor<IMessage> orderProvider = Invokers.CreateForHandlerOrderProvider(messageType, orderer);
      return orderProvider.GetHandlerOrder();
    }
  }

  public class MessageDispatcher : IMessageDispatcher
  {
    static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(MessageDispatcher));
    readonly IMachineContainer _container;
    readonly HandlerDiscoverer _handlerDiscoverer;
    readonly IMessageAspectsProvider _messageAspectsProvider;

    public MessageDispatcher(IMachineContainer container, IMessageAspectsProvider messageAspectsProvider)
    {
      _container = container;
      _messageAspectsProvider = messageAspectsProvider;
      _handlerDiscoverer = new HandlerDiscoverer(container);
    }

    private void Dispatch(IMessage message)
    {
      _log.Info("Dispatching " + message);
      foreach (MessageHandlerType messageHandlerType in _handlerDiscoverer.GetHandlerTypesFor(message.GetType()))
      {
        object handler = _container.Resolve.Object(messageHandlerType.TargetType);
        Consumes<IMessage>.All invoker = Invokers.CreateForHandler(messageHandlerType.TargetExpectsMessageOfType, handler);
        HandlerInvocation invocation = messageHandlerType.ToInvocation(message, handler, invoker, _messageAspectsProvider.DefaultAspects());
        invocation.Continue();
      }
    }

    public void Dispatch(IMessage[] messages)
    {
      foreach (IMessage message in messages)
      {
        Dispatch(message);
      }
    }
  }

  public static class InvocationMappings
  {
    public static HandlerInvocation ToInvocation(this MessageHandlerType messageHandlerType, IMessage message, object handler, Consumes<IMessage>.All invoker, Queue<IMessageAspect> aspects)
    {
      return new HandlerInvocation(message, messageHandlerType.TargetExpectsMessageOfType, messageHandlerType.TargetType, handler, invoker, aspects);
    }
  }

  public class HandlerInvocation
  {
    readonly IMessage _message;
    readonly Type _messageType;
    readonly Type _handlerType;
    readonly object _handler;
    readonly Queue<IMessageAspect> _aspects;
    readonly Consumes<IMessage>.All _invoker;

    public IMessage Message
    {
      get { return _message; }
    }

    public Type MessageType
    {
      get { return _messageType; }
    }

    public Type HandlerType
    {
      get { return _handlerType; }
    }

    public object Handler
    {
      get { return _handler; }
    }

    public HandlerInvocation(IMessage message, Type messageType, Type handlerType, object handler, Consumes<IMessage>.All invoker, Queue<IMessageAspect> aspects)
    {
      _message = message;
      _aspects = aspects;
      _messageType = messageType;
      _handlerType = handlerType;
      _handler = handler;
      _invoker = invoker;
    }

    public void Continue()
    {
      if (_aspects.Count > 0)
      {
        _aspects.Dequeue().Continue(this);
      }
      else
      {
        _invoker.Consume(_message);
      }
    }
  }

  public interface IMessageAspectsProvider
  {
    Queue<IMessageAspect> DefaultAspects();
  }

  public class DefaultMessageAspectsProvider : IMessageAspectsProvider
  {
    readonly IMachineContainer _container;

    public DefaultMessageAspectsProvider(IMachineContainer container)
    {
      _container = container;
    }

    protected virtual Type[] AspectTypes
    {
      get { return new[] { typeof(SagaAspect) }; }
    }

    public Queue<IMessageAspect> DefaultAspects()
    {
      Queue<IMessageAspect> aspects = new Queue<IMessageAspect>();
      foreach (Type type in this.AspectTypes)
      {
        aspects.Enqueue((IMessageAspect)_container.Resolve.Object(type));
      }
      return aspects;
    }
  }

  public interface IMessageAspect
  {
    void Continue(HandlerInvocation invocation);
  }
}