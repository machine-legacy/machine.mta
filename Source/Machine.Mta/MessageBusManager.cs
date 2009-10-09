using System;
using System.Collections.Generic;
using System.Linq;

using Machine.Core;
using Machine.Container;
using Machine.Mta.Dispatching;
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

    public IMessageBus AddMessageBus(BusProperties properties)
    {
      var bus = _messageBusFactory.CreateMessageBus(properties.ListenAddress, properties.PoisonAddress, new AllHandlersInContainer(_container), new ThreadPoolConfiguration(
        properties.NumberOfWorkerThreads,
        properties.NumberOfWorkerThreads
      ));
      _buses.Add(bus);
      return bus;
    }

    public void EachBus(Action<IMessageBus> action)
    {
      _buses.Each(action);
    }

    public void Dispose()
    {
      EachBus(b => b.Dispose());
    }
  }
}
