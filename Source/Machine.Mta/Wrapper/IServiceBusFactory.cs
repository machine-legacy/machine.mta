using System;
using System.Collections.Generic;

using MassTransit.ServiceBus;

namespace Machine.Mta.Wrapper
{
  public interface IServiceBusFactory
  {
    IServiceBus CreateServiceBus(EndpointName endpointName);
  }
}
