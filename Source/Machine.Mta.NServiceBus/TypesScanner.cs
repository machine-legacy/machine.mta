using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Machine.Container.Model;
using Machine.Container.Services;
using Machine.Mta.Dispatching;

using NServiceBus.Saga;

namespace Machine.Mta
{
  public static class TypesScanner
  {
    public static IEnumerable<Type> MessagesFrom(params Assembly[] messageAssemblies)
    {
      return PossiblyDuplicateMessagesFrom(messageAssemblies).Distinct();
    }

    static IEnumerable<Type> PossiblyDuplicateMessagesFrom(params Assembly[] messageAssemblies)
    {
      foreach (var assembly in messageAssemblies)
      {
        foreach (var type in assembly.GetTypes())
        {
          if (typeof(IMessage).IsAssignableFrom(type))
          {
            yield return type;
          }
        }
      }
    }

    public static IEnumerable<Type> Sagas(this IMachineContainer container)
    {
      foreach (ServiceRegistration registration in container.RegisteredServices)
      {
        if (typeof(ISaga).IsAssignableFrom(registration.ServiceType))
        {
          yield return registration.ServiceType;
        }
      }
    }

    public static IEnumerable<Type> Handlers(this IMachineContainer container)
    {
      foreach (var handlerType in AllMessageHandlerTypes(container))
      {
        if (typeof(NServiceBus.IMessage).IsAssignableFrom(handlerType.TargetExpectsMessageOfType))
        {
          yield return MessageHandlerProxies.For(handlerType.TargetExpectsMessageOfType, handlerType.TargetType);
        }
      }
    }

    static IEnumerable<MessageHandlerType> AllMessageHandlerTypes(IMachineContainer container)
    {
      foreach (var handlerType in new AllHandlersInContainer(container).HandlerTypes())
      {
        var handlerConsumes = handlerType.AllGenericVariations(typeof(IConsume<>));
        foreach (var type in handlerConsumes)
        {
          yield return new MessageHandlerType(handlerType, type);
        }
      }
    }
  }
}