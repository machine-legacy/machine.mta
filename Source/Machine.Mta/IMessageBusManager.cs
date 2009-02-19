using System;
using System.Collections.Generic;

using Machine.Utility.ThreadPool;

namespace Machine.Mta
{
  public interface IMessageBusManager
  {
    IMessageBus DefaultBus
    {
      get;
    }
    IMessageBus AddMessageBus(EndpointAddress address, EndpointAddress poisonAddress);
    IMessageBus AddMessageBus(EndpointAddress address, EndpointAddress poisonAddress, ThreadPoolConfiguration threadPoolConfiguration);
    IMessageBus UseSingleBus(EndpointAddress address, EndpointAddress poisonAddress);
    void EachBus(Action<IMessageBus> action);
  }
}
