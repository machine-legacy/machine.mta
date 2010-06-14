using System;
using System.Collections.Generic;
using System.Linq;

using Machine.Container;
using Machine.Core;

using NServiceBus;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.ObjectBuilder.Machine.Config;
using NServiceBus.Sagas.Impl;
using NServiceBus.Unicast.Transport.RabbitMQ.Config;
using Configure = NServiceBus.Configure;

namespace Machine.Mta
{
  public interface IMtaBusConfiguration
  {
    IEnumerable<Type> Types();
    Configure Configure(Configure configure);
  }

  public class NsbMessageBusFactory : INsbMessageBusFactory, IDisposable
  {
    readonly IMtaBusConfiguration _container;
    readonly IMessageRegisterer _registerer;
    readonly IMessageRouting _messageRouting;
    readonly List<NsbBus> _all = new List<NsbBus>();

    public NsbMessageBusFactory(IMtaBusConfiguration container, IMessageRegisterer registerer, IMessageRouting messageRouting)
    {
      _container = container;
      _messageRouting = messageRouting;
      _registerer = registerer;
    }

    NsbBus CreateMsmq(BusProperties properties)
    {
      //                _container.Handlers().
      //          Union(_container.Finders()).
      //          Union(_container.Sagas()).
      var types =       _container.Types().
                  Union(_registerer.MessageTypes).
                  Union(properties.AdditionalTypes).ToList();
      var configure = _container.Configure(Configure
        .With(types)
        /*.MachineBuilder(_container*/)
        .CustomizedXmlSerializer()
        .CustomizedMsmqTransport()
          .MtaHeaderSerializer()
          .On(properties.ListenAddress, properties.PoisonAddress)
        .Sagas()
        .CustomizedUnicastBus()
          .LoadMessageHandlers(First<GridInterceptingMessageHandler>.Then<SagaMessageHandler>())
          .WithMessageRoutes(_messageRouting);
      return Add(configure.CreateBus());
    }

    NsbBus CreateAmqp(BusProperties properties)
    {
      //                _container.Handlers().
      //          Union(_container.Finders()).
      //          Union(_container.Sagas()).
      var types =       _container.Types().
                  Union(_registerer.MessageTypes).
                  Union(properties.AdditionalTypes).ToList();
      var configure = _container.Configure(Configure
        .With(types)
        /*.MachineBuilder(_container*/)
        .CustomizedXmlSerializer()
        .AmqpTransport()
          .On(properties.ListenAddress.ToString(), properties.PoisonAddress.ToString())
        .Sagas()
        .CustomizedUnicastBus()
          .LoadMessageHandlers(First<GridInterceptingMessageHandler>.Then<SagaMessageHandler>())
          .WithMessageRoutes(_messageRouting);
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
