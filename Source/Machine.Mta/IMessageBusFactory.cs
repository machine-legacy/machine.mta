using System;
using System.Collections.Generic;

namespace Machine.Mta
{
  public interface IMessageBusFactory
  {
    IMessageBus CreateMessageBus(EndpointAddress listeningOnEndpointAddress, EndpointAddress poisonEndpointAddress);
  }
}
