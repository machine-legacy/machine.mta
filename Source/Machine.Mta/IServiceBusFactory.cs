using System;
using System.Collections.Generic;

using MassTransit.ServiceBus;

namespace Machine.Mta
{
  public interface IServiceBusFactory
  {
    IServiceBus CreateServiceBus(EndpointName endpointName);
  }
}
