using System;
using System.Collections.Generic;

using Machine.Container;

namespace Machine.Mta.Dispatching
{
  public class AllHandlersInContainer : IProvideHandlerTypes
  {
    readonly IInspectBusTypes _inspectBusTypes;
    readonly IMachineContainer _container;

    public AllHandlersInContainer(IInspectBusTypes inspectBusTypes, IMachineContainer container)
    {
      _inspectBusTypes = inspectBusTypes;
      _container = container;
    }

    public IEnumerable<Type> HandlerTypes()
    {
      foreach (var registration in _container.RegisteredServices)
      {
        var type = registration.ServiceType;
        if (_inspectBusTypes.IsConsumer(type))
        {
          yield return registration.ServiceType;
        }
      }
    }
  }
}