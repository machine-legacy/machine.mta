using System;
using System.Collections.Generic;
using Machine.Mta.Sagas;

namespace Machine.Mta
{
  public class InspectBusTypes : IInspectBusTypes
  {
    public static readonly IInspectBusTypes Instance = new InspectBusTypes();

    public bool IsConsumer(Type type)
    {
      return type.IsImplementationOfGenericType(typeof(IConsume<>));
    }

    public bool IsSagaConsumer(Type type)
    {
      return typeof(ISagaHandler).IsAssignableFrom(type);
    }
  }
}
