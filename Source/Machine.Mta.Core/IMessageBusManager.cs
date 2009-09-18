using System;
using System.Collections.Generic;

using Machine.Mta.Dispatching;
using Machine.Utility.ThreadPool;

namespace Machine.Mta
{
  public interface IMessageBusManager
  {
    IMessageBus DefaultBus
    {
      get;
    }

    IMessageBus AddSendOnlyMessageBus();
    IMessageBus AddMessageBus(EndpointAddress address, EndpointAddress poisonAddress);
    IMessageBus AddMessageBus(EndpointAddress address, EndpointAddress poisonAddress, IProvideHandlerTypes handlerTypes);
    IMessageBus AddMessageBus(EndpointAddress address, EndpointAddress poisonAddress, IProvideHandlerTypes handlerTypes, ThreadPoolConfiguration threadPoolConfiguration);
    IMessageBus AddMessageBus(EndpointAddress address, EndpointAddress poisonAddress, ThreadPoolConfiguration threadPoolConfiguration);
    IMessageBus UseSingleBus(EndpointAddress address, EndpointAddress poisonAddress);
    void EachBus(Action<IMessageBus> action);
  }
}
