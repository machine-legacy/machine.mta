using System;
using System.Collections.Generic;

using Machine.Mta.Dispatching;

namespace Machine.Mta
{
  public interface IMessageBusFactory
  {
    IMessageBus CreateMessageBus(EndpointAddress listeningOnEndpointAddress, EndpointAddress poisonEndpointAddress, IProvideHandlerTypes handlerTypes);
  }
}
