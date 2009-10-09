using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Machine.Container.Model;
using Machine.Container;
using Machine.Mta.Dispatching;

using NServiceBus;
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

    public static IEnumerable<Type> Finders(this IMachineContainer container)
    {
      foreach (ServiceRegistration registration in container.RegisteredServices)
      {
        if (IsAllowableImplementationOf(registration.ServiceType, typeof(IFinder)))
        {
          yield return registration.ServiceType;
        }
      }
    }

    private static bool IsAllowableImplementationOf(Type type, Type source)
    {
      return (((source.IsAssignableFrom(type) && (type != source)) && (!type.IsAbstract && !type.IsInterface)) && !type.IsGenericType);
    }

    public static IEnumerable<Type> Handlers(this IMachineContainer container)
    {
      foreach (var handlerType in AllConsumerTypes(container))
      {
        if (typeof(NServiceBus.IMessage).IsAssignableFrom(handlerType.TargetExpectsMessageOfType))
        {
          yield return MessageHandlerProxies.For(handlerType.TargetExpectsMessageOfType, handlerType.TargetType);
        }
      }

      foreach (var handlerType in AllMessageHandlerTypes(container))
      {
        yield return handlerType.TargetType;
      }
    }

    static IEnumerable<MessageHandlerType> AllMessageHandlerTypes(IMachineContainer container)
    {
      foreach (var handlerType in new AllHandlersInContainer(NsbInspectBusTypes.Instance, container).HandlerTypes())
      {
        var messageHandlers = handlerType.AllGenericVariations(typeof(IMessageHandler<>));
        foreach (var type in messageHandlers)
        {
          yield return new MessageHandlerType(handlerType, type);
        }
      }
    }

    static IEnumerable<MessageHandlerType> AllConsumerTypes(IMachineContainer container)
    {
      foreach (var handlerType in new AllHandlersInContainer(NsbInspectBusTypes.Instance, container).HandlerTypes())
      {
        var consumers = handlerType.AllGenericVariations(typeof(IConsume<>));
        foreach (var type in consumers)
        {
          yield return new MessageHandlerType(handlerType, type);
        }
      }
    }
  }
}