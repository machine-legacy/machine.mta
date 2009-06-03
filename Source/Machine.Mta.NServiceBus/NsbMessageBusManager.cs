using System;
using System.Collections.Generic;

using Machine.Mta.Dispatching;
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

    public IMessageBus AddMessageBus(EndpointAddress address, EndpointAddress poisonAddress)
    {
      var bus = _messageBusFactory.Create(address, poisonAddress, new Type[0]);
      return new NServiceBusMessageBus(_routing, bus);
    }

    public IMessageBus AddMessageBus(EndpointAddress address, EndpointAddress poisonAddress, IProvideHandlerTypes handlerTypes)
    {
      throw new NotImplementedException();
    }

    public IMessageBus UseSingleBus(EndpointAddress address, EndpointAddress poisonAddress)
    {
      throw new NotImplementedException();
    }

    public void EachBus(Action<IMessageBus> action)
    {
      _messageBusFactory.EachBus(b => action(new NServiceBusMessageBus(_routing, b)));
    }

    public IMessageBus AddMessageBus(EndpointAddress address, EndpointAddress poisonAddress, ThreadPoolConfiguration threadPoolConfiguration)
    {
      var bus = _messageBusFactory.Create(address, poisonAddress, new Type[0]);
      return new NServiceBusMessageBus(_routing, bus);
    }

    public IMessageBus AddMessageBus(EndpointAddress address, EndpointAddress poisonAddress, IProvideHandlerTypes handlerTypes, ThreadPoolConfiguration threadPoolConfiguration)
    {
      throw new NotImplementedException();
    }
  }
}
