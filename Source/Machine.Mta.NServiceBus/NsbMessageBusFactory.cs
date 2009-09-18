using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Container;
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
    readonly IMessageRegisterer _registerer;
    readonly IMessageDestinations _messageDestinations;
    readonly List<NsbBus> _all = new List<NsbBus>();

    public NsbMessageBusFactory(IMachineContainer container, IMessageRegisterer registerer, IMessageDestinations messageDestinations)
    {
      _container = container;
      _messageDestinations = messageDestinations;
      _registerer = registerer;
    }

    public NsbBus Create(IEnumerable<Type> additionalTypes)
    {
      var types =       _container.Handlers().
                  Union(_container.Finders()).
                  Union(_container.Sagas()).
                  Union(_registerer.MessageTypes).
                  Union(additionalTypes).ToList();
      return Add(EndpointAddress.Null, EndpointAddress.Null, Configure
        .With(types)
        .MachineBuilder(_container)
        .StaticSubscriptionStorage()
        .XmlSerializer()
        .MsmqTransport()
        .Sagas()
        .UnicastBus()
          .LoadMessageHandlers(First<GridInterceptingMessageHandler>.Then<SagaMessageHandler>())
          .WithMessageRoutes(_messageDestinations)
        .CreateBus());
    }

    public NsbBus Create(EndpointAddress listenAddress, EndpointAddress poisonAddress, IEnumerable<Type> additionalTypes)
    {
      var types =       _container.Handlers().
                  Union(_container.Finders()).
                  Union(_container.Sagas()).
                  Union(_registerer.MessageTypes).
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

    public void Start()
    {
      _startableBus.Start();
    }
  }

  public interface INsbMessageBusFactory
  {
    NsbBus Create(EndpointAddress listenAddress, EndpointAddress poisonAddress, IEnumerable<Type> additionalTypes);
    NsbBus Create(IEnumerable<Type> additionalTypes);
    void EachBus(Action<IStartableBus> action);
    void EachBus(Action<NsbBus> action);
    NsbBus CurrentBus();
  }

  public class Fun
  {
    public void Run()
    {
      var container = new MachineContainer();
      container.Initialize();
      container.PrepareForServices();
      // container.Register.Type<StaticSubscriptionStorage>();
      container.Start();
      var registerer = new MessageRegisterer();
      var destinations = new MessageDestinations();
      var factory = new NsbMessageBusFactory(container, registerer, destinations);
      var bus = factory.Create(new Type[0]);
      bus.Start();
      bus.Bus.Send("");
    }
  }
}
