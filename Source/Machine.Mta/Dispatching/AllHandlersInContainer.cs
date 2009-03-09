using System;
using System.Collections.Generic;

using Machine.Container.Model;
using Machine.Container.Services;

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
      foreach (ServiceRegistration registration in _container.RegisteredServices)
      {
        if (registration.ServiceType.IsImplementationOfGenericType(typeof(IConsume<>)))
        {
          yield return registration.ServiceType;
        }
      }
    }
  }
}