using System;
using System.Collections.Generic;
using System.Linq;

using Machine.Container;

namespace Machine.Mta.Dispatching
{
  public class HandlerDiscoverer
  {
    readonly IMachineContainer _container;
    readonly IProvideHandlerTypes _handlerTypes;

    public HandlerDiscoverer(IMachineContainer container, IProvideHandlerTypes handlerTypes)
    {
      _container = container;
      _handlerTypes = handlerTypes;
    }

    public IEnumerable<MessageHandlerType> GetHandlerTypesFor(Type messageType)
    {
      List<MessageHandlerType> messageHandlerTypes = new List<MessageHandlerType>();

      foreach (Type handlerType in _handlerTypes.HandlerTypes())
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
      IProvideHandlerOrder orderProvider = Invokers.CreateForHandlerOrderProvider(messageType, orderer);
      return orderProvider.GetHandlerOrder();
    }
  }
}