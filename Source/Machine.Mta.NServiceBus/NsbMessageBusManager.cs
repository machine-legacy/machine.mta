using System;
using System.Collections.Generic;

using Machine.Utility.ThreadPool;

namespace Machine.Mta
{
  public class NsbMessageBusManager : IMessageBusManager
  {
    readonly INsbMessageBusFactory _messageBusFactory;
    readonly IMessageRouting _routing;

    public NsbMessageBusManager(INsbMessageBusFactory messageBusFactory, IMessageRouting routing)
    {
      _messageBusFactory = messageBusFactory;
      _routing = routing;
    }

    public IMessageBus DefaultBus
    {
      get { return new NServiceBusMessageBus(_routing, _messageBusFactory.CurrentBus()); }
    }

    public IMessageBus AddMessageBus(BusProperties properties)
    {
      var bus = _messageBusFactory.Create(properties);
      return new NServiceBusMessageBus(_routing, bus);
    }

    public void EachBus(Action<IMessageBus> action)
    {
      _messageBusFactory.EachBus(b => action(new NServiceBusMessageBus(_routing, b)));
    }
  }
}
