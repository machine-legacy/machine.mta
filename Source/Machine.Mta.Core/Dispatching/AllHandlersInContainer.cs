using System;
using System.Collections.Generic;

using Machine.Container;

using NServiceBus;

namespace Machine.Mta.Dispatching
{
  public class AllHandlersInContainer : IProvideHandlerTypes
  {
    readonly IMachineContainer _container;

    public AllHandlersInContainer(IMachineContainer container)
    {
      _container = container;
    }

    public IEnumerable<Type> HandlerTypes()
    {
      foreach (var registration in _container.RegisteredServices)
      {
        var type = registration.ServiceType;
        if (type.IsImplementationOfGenericType(typeof(IConsume<>)) || type.IsImplementationOfGenericType(typeof(IMessageHandler<>)))
        {
          yield return registration.ServiceType;
        }
      }
    }
  }
}