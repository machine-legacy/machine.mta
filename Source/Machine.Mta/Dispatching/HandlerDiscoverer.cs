using System;
using System.Collections.Generic;
using System.Linq;

using Machine.Container.Model;
using Machine.Container.Services;

namespace Machine.Mta.Dispatching
{
  public class HandlerDiscoverer
  {
    private readonly IMachineContainer _container;

    public HandlerDiscoverer(IMachineContainer container)
    {
      _container = container;
    }

    private IEnumerable<Type> TypesThatAreHandlers()
    {
      foreach (ServiceRegistration registration in _container.RegisteredServices)
      {
        if (registration.ServiceType.IsImplementationOfGenericType(typeof(IConsume<>)))
        {
          yield return registration.ServiceType;
        }
      }
    }

    public IEnumerable<MessageHandlerType> GetHandlerTypesFor(Type messageType)
    {
      List<MessageHandlerType> messageHandlerTypes = new List<MessageHandlerType>();

      foreach (Type handlerType in TypesThatAreHandlers())
      {
        IEnumerable<Type> handlerConsumes = handlerType.AllGenericVariations(typeof(IConsume<>)).BiggerThan(typeof(IConsume<>).MakeGenericType(messageType));
        Type smallerType = handlerConsumes.SmallerType();
        if (smallerType != null)
        {
          messageHandlerTypes.Add(new MessageHandlerType(handlerType, smallerType));
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
                                                        return type.IsGenericlyCompatible(typeof(IProvideHandlerOrderFor<>).MakeGenericType(messageType));
      }).FirstOrDefault();
      if (orderer == null)
      {
        return new List<Type>();
      }
      IProvideHandlerOrderFor<IMessage> orderProvider = Invokers.CreateForHandlerOrderProvider(messageType, orderer);
      return orderProvider.GetHandlerOrder();
    }
  }
}