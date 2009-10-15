using System;
using System.Collections.Generic;
using System.Linq;

using Machine.Container;
using Machine.Core;
using Machine.Mta.Config;

using NServiceBus;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.Sagas.Impl;
using NServiceBus.Unicast.Transport.RabbitMQ.Config;
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

    NsbBus CreateMsmq(BusProperties properties)
    {
      var types =       _container.Handlers().
                  Union(_container.Finders()).
                  Union(_container.Sagas()).
                  Union(_registerer.MessageTypes).
                  Union(properties.AdditionalTypes).ToList();
      var configure = Configure
        .With(types)
        .MachineBuilder(_container)
        .StaticSubscriptionStorage()
        .XmlSerializer()
        .MsmqTransport()
          .On(properties.ListenAddress, properties.PoisonAddress)
        .Sagas()
        .UnicastBus()
          .LoadMessageHandlers(First<GridInterceptingMessageHandler>.Then<SagaMessageHandler>())
          .WithMessageRoutes(_messageDestinations);
      return Add(configure.CreateBus());
    }

    NsbBus CreateAmqp(BusProperties properties)
    {
      var types =       _container.Handlers().
                  Union(_container.Finders()).
                  Union(_container.Sagas()).
                  Union(_registerer.MessageTypes).
                  Union(properties.AdditionalTypes).ToList();
      var configure = Configure
        .With(types)
        .MachineBuilder(_container)
        .StaticSubscriptionStorage()
        .XmlSerializer()
        .AmqpTransport()
          .On(properties.ListenAddress.ToString(), properties.PoisonAddress.ToString())
        .Sagas()
        .UnicastBus()
          .LoadMessageHandlers(First<GridInterceptingMessageHandler>.Then<SagaMessageHandler>())
          .WithMessageRoutes(_messageDestinations);
      return Add(configure.CreateBus());
    }

    public NsbBus Create(BusProperties properties)
    {
      if (properties.TransportType == TransportType.RabbitMq)
      {
        return CreateAmqp(properties);
      }
      return CreateMsmq(properties);
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

    NsbBus Add(IStartableBus bus)
    {
      var nsbBus = new NsbBus(bus);
      _all.Add(nsbBus);
      return nsbBus;
    }

    public void Dispose()
    {
      EachBus(b => b.Dispose());
    }
  }

  public interface INsbMessageBusFactory
  {
    NsbBus Create(BusProperties properties);
    void EachBus(Action<IStartableBus> action);
    void EachBus(Action<NsbBus> action);
    NsbBus CurrentBus();
  }
}
