using System;
using System.Collections.Generic;

namespace Machine.Mta
{
  public interface IMessageBusManager
  {
    IMessageBus UseSingleBus(EndpointName listeningEndpoint, EndpointName poisonEndpoint);
  }
}
