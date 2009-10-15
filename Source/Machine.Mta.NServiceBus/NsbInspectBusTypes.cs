using System;
using System.Collections.Generic;
using NServiceBus;

namespace Machine.Mta
{
  public class NsbInspectBusTypes : IInspectBusTypes
  {
    public static readonly IInspectBusTypes Instance = new NsbInspectBusTypes();

    public bool IsConsumer(Type type)
    {
      return type.IsImplementationOfGenericType(typeof(IConsume<>)) ||
             type.IsImplementationOfGenericType(typeof(IMessageHandler<>));
    }

    public bool IsSagaConsumer(Type type)
    {
      return typeof(NServiceBus.Saga.ISaga).IsAssignableFrom(type);
    }
  }
}
