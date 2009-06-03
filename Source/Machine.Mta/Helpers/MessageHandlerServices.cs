using System;
using System.Reflection;

using NServiceBus;

using Machine.Container;
using Machine.Container.Plugins;
using Machine.Mta.Sagas;

namespace Machine.Mta.Helpers
{
  public class MessageHandlerServices : IServiceCollection
  {
    readonly Assembly[] _assemblies;

    public MessageHandlerServices(params Assembly[] assemblies)
    {
      _assemblies = assemblies;
    }

    public void RegisterServices(ContainerRegisterer register)
    {
      foreach (Assembly assembly in _assemblies)
      {
        foreach (Type type in assembly.GetTypes())
        {
          if (IsHandlerOrConsumer(type))
          {
            if (IsSagaHandler(type))
            {
              register.Type(type).AsTransient();
            }
            else
            {
              register.Type(type);
            }
          }
        }
      }
    }

    static bool IsHandlerOrConsumer(Type type)
    {
      return type.IsImplementationOfGenericType(typeof(IConsume<>)) || type.IsImplementationOfGenericType(typeof(IMessageHandler<>));
    }

    static bool IsSagaHandler(Type type)
    {
      return typeof(ISagaHandler).IsAssignableFrom(type) || typeof(NServiceBus.Saga.ISaga).IsAssignableFrom(type);
    }
  }
}