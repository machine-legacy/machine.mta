using System;
using System.Collections.Generic;

namespace Machine.Mta
{
  public interface IMessageBusManager
  {
    IMessageBus UseSingleBus(EndpointAddress listeningEndpoint, EndpointAddress poisonEndpoint);
  }
}
