using System;
using System.Collections.Generic;

using Machine.Mta.Dispatching;
using Machine.Utility.ThreadPool;

namespace Machine.Mta
{
  public interface IMessageBusFactory
  {
    IMessageBus CreateMessageBus(EndpointAddress listeningOnEndpointAddress, EndpointAddress poisonEndpointAddress, IProvideHandlerTypes handlerTypes, ThreadPoolConfiguration threadPoolConfiguration);
  }
}
