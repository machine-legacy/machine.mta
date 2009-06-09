using System;
using System.Collections.Generic;
using System.Linq;

using Machine.Container.Services;
using Machine.Core;
using Machine.Mta.Config;

using NServiceBus;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.Saga;
using Configure = NServiceBus.Configure;

namespace Machine.Mta
{
  public class NsbMessageBusFactory : INsbMessageBusFactory, IDisposable
  {
    readonly IMachineContainer _container;
    readonly NsbMessageRegisterer _messageRegisterer;
    readonly IMessageDestinations _messageDestinations;
    readonly List<NsbBus> _all = new List<NsbBus>();

    public NsbMessageBusFactory(IMachineContainer container, NsbMessageRegisterer messageRegisterer, IMessageDestinations messageDestinations)
    {
      _container = container;
      _messageDestinations = messageDestinations;
      _messageRegisterer = messageRegisterer;
    }

    public NsbBus Create(EndpointAddress listenAddress, EndpointAddress poisonAddress, IEnumerable<Type> additionalTypes)
    {
      var types =       _container.Handlers().
                  Union(_container.Finders()).
                  Union(_container.Sagas()).
                  Union(_messageRegisterer.MessageTypes).
                  Union(additionalTypes).ToList();
      return Add(listenAddress, poisonAddress, Configure
        .With(types)
        .MachineBuilder(_container)
        .StaticSubscriptionStorage()
        .XmlSerializer()
        .MsmqTransport()
          .On(listenAddress, poisonAddress)
        .Sagas()
        .UnicastBus()
          .LoadMessageHandlers(First<GridInterceptingMessageHandler>.Then<SagaMessageHandler>())
          .WithMessageRoutes(_messageDestinations)
        .CreateBus());
    }

    public IStartableBus AddBus(IStartableBus bus)
    {
      // var nsbBus = new NsbBus();
      // _all.Add(nsbBus);
      return bus;
    }

    public void EachBus(Action<IStartableBus> action)
    {
      _all.Each(b => action(b.StartableBus));
    }

    public void EachBus(Action<NsbBus> action)
    {
      _all.Each(action);
    }

    public NsbBus CurrentBus()
    {
      return _all.First();
    }

    NsbBus Add(EndpointAddress listenAddress, EndpointAddress poisonAddress, IStartableBus bus)
    {
      var nsbBus = new NsbBus(listenAddress, poisonAddress, bus);
      _all.Add(nsbBus);
      return nsbBus;
    }

    public void Dispose()
    {
      EachBus(b => b.Dispose());
    }
  }

  public class NsbBus
  {
    readonly EndpointAddress _listenAddress;
    readonly EndpointAddress _poisonAddress;
    readonly IStartableBus _startableBus;

    public EndpointAddress ListenAddress
    {
      get { return _listenAddress; }
    }

    public EndpointAddress PoisonAddress
    {
      get { return _poisonAddress; }
    }

    public IStartableBus StartableBus
    {
      get { return _startableBus; }
    }

    public IBus Bus
    {
      get { return _startableBus.Start(); }
    }

    public NsbBus(EndpointAddress listenAddress, EndpointAddress poisonAddress, IStartableBus startableBus)
    {
      _listenAddress = listenAddress;
      _poisonAddress = poisonAddress;
      _startableBus = startableBus;
    }
  }

  public interface INsbMessageBusFactory
  {
    NsbBus Create(EndpointAddress listenAddress, EndpointAddress poisonAddress, IEnumerable<Type> additionalTypes);
    IStartableBus AddBus(IStartableBus bus);
    void EachBus(Action<IStartableBus> action);
    void EachBus(Action<NsbBus> action);
    NsbBus CurrentBus();
  }
}
