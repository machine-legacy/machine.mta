using System;
using System.Collections.Generic;

namespace Machine.Mta
{
  public interface IMessageBusManager
  {
    IMessageBus DefaultBus
    {
      get;
    }
    IMessageBus AddMessageBus(EndpointAddress address, EndpointAddress poisonAddress);
    IMessageBus UseSingleBus(EndpointAddress address, EndpointAddress poisonAddress);
    void EachBus(Action<IMessageBus> action);
  }
}
