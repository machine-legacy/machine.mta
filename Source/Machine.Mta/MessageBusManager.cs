using System;
using System.Collections.Generic;
using System.Linq;

using Machine.Container.Services;
using Machine.Core;
using Machine.Utility.ThreadPool;

namespace Machine.Mta
{
  public class MessageBusManager : IMessageBusManager, IDisposable
  {
    readonly IMachineContainer _container;
    readonly IMessageBusFactory _messageBusFactory;
    readonly List<IMessageBus> _buses = new List<IMessageBus>();

    public IMessageBus DefaultBus
    {
      get { return _buses.First(); }
    }

    public MessageBusManager(IMessageBusFactory messageBusFactory, IMachineContainer container)
    {
      _messageBusFactory = messageBusFactory;
      _container = container;
    }

    public IMessageBus AddMessageBus(EndpointAddress address, EndpointAddress poisonAddress)
    {
      return AddMessageBus(address, poisonAddress, new ThreadPoolConfiguration(1, 1));
    }

    public IMessageBus AddMessageBus(EndpointAddress address, EndpointAddress poisonAddress, ThreadPoolConfiguration threadPoolConfiguration)
    {
      MessageBus bus = (MessageBus)_messageBusFactory.CreateMessageBus(address, poisonAddress);
      bus.ChangeThreadPoolConfiguration(threadPoolConfiguration);
      _buses.Add(bus);
      return bus;
    }

    public IMessageBus UseSingleBus(EndpointAddress address, EndpointAddress poisonAddress)
    {
      IMessageBus bus = AddMessageBus(address, poisonAddress);
      _container.Register.Type<IMessageBus>().Is(bus);
      return bus;
    }

    public void EachBus(Action<IMessageBus> action)
    {
      _buses.Each(action);
    }

    public void Dispose()
    {
      foreach (IMessageBus bus in _buses)
      {
        bus.Dispose();
      }
    }
  }
}
