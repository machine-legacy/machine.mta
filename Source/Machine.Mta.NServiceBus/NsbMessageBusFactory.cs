using System;
using System.Collections.Generic;
using System.Linq;

using Machine.Container.Services;
using Machine.Core;
using Machine.Mta.Config;

using NServiceBus;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast;
using NServiceBus.Unicast.Config;
using NServiceBus.Unicast.Subscriptions.Msmq;
using NServiceBus.Unicast.Transport.Msmq;

namespace Machine.Mta
{
  public class NsbMessageBusFactory : INsbMessageBusFactory, IDisposable
  {
    readonly IMachineContainer _container;
    readonly NsbMessageRegisterer _messageRegisterer;
    readonly List<NsbBus> _all = new List<NsbBus>();

    public NsbMessageBusFactory(IMachineContainer container, NsbMessageRegisterer messageRegisterer)
    {
      _container = container;
      _messageRegisterer = messageRegisterer;
    }

    public NsbBus Create(EndpointAddress listenAddress, EndpointAddress poisonAddress, IEnumerable<Type> additionalTypes)
    {
      return Add(listenAddress, poisonAddress, Configure
        .With(_container.Handlers().Union(_messageRegisterer.MessageTypes).Union(additionalTypes).ToList())
        .MachineBuilder(_container)
        .XmlSerializer()
        .MsmqTransport()
          .On(listenAddress, poisonAddress)
        .UnicastBus()
          .LoadMessageHandlers()
        .CreateBus());
    }

    public NsbBus Create(EndpointAddress subscriptionStorageAddress, EndpointAddress listenAddress, EndpointAddress poisonAddress, IEnumerable<Type> additionalTypes)
    {
      return Add(listenAddress, poisonAddress, Configure
        .With(_container.Handlers().Union(_messageRegisterer.MessageTypes).Union(additionalTypes).ToList())
        .MachineBuilder(_container)
        .MsmqSubscriptionStorage()
          .In(subscriptionStorageAddress)
        .XmlSerializer()
        .MsmqTransport()
          .On(listenAddress, poisonAddress)
        .UnicastBus()
          .LoadMessageHandlers()
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
  }

  public interface INsbMessageBusFactory
  {
    NsbBus Create(EndpointAddress listenAddress, EndpointAddress poisonAddress, IEnumerable<Type> additionalTypes);
    NsbBus Create(EndpointAddress subscriptionStorageAddress, EndpointAddress listenAddress, EndpointAddress poisonAddress, IEnumerable<Type> additionalTypes);
    void EachBus(Action<IStartableBus> action);
    void EachBus(Action<NsbBus> action);
    NsbBus CurrentBus();
  }

  public static class MyConfigure
  {
    public static MyConfigMsmqTransport MsmqTransport(this Configure config)
    {
      var cfg = new MyConfigMsmqTransport();
      cfg.Configure(config);
      return cfg;
    }

    public static MyConfigUnicastBus UnicastBus(this Configure config)
    {
      var cfg = new MyConfigUnicastBus();
      cfg.Configure(config);
      return cfg;
    }

    public static MyConfigMsmqSubscriptionStorage MsmqSubscriptionStorage(this Configure config)
    {
      var cfg = new MyConfigMsmqSubscriptionStorage();
      cfg.Configure(config);
      return cfg;
    }
  }

  public class MyConfigMsmqTransport : Configure
  {
    private IComponentConfig<MsmqTransport> _config;

    public void Configure(Configure config)
    {
      this.Builder = config.Builder;
      this.Configurer = config.Configurer;

      _config = Configurer.ConfigureComponent<MsmqTransport>(ComponentCallModelEnum.Singleton);
      _config.ConfigureProperty(t => t.IsTransactional, true);
      _config.ConfigureProperty(t => t.PurgeOnStartup, false);
      _config.ConfigureProperty(t => t.NumberOfWorkerThreads, 1);
      _config.ConfigureProperty(t => t.MaxRetries, 3);
    }

    public MyConfigMsmqTransport On(EndpointAddress listenAddress, EndpointAddress poisonAddress)
    {
      _config.ConfigureProperty(t => t.InputQueue, listenAddress.ToNsbAddress());
      _config.ConfigureProperty(t => t.ErrorQueue, poisonAddress.ToNsbAddress());
      return this;
    }
  }

  public class MyConfigUnicastBus : Configure
  {
    private IComponentConfig<UnicastBus> _config;

    public void Configure(Configure config)
    {
      this.Builder = config.Builder;
      this.Configurer = config.Configurer;

      _config = Configurer.ConfigureComponent<UnicastBus>(ComponentCallModelEnum.Singleton);
      _config.ConfigureProperty(b => b.ForwardReceivedMessagesTo, "");
      _config.ConfigureProperty(b => b.DistributorControlAddress, "");
      _config.ConfigureProperty(b => b.DistributorDataAddress, "");
      _config.ConfigureProperty(b => b.ImpersonateSender, false);
    }

    public MyConfigUnicastBus LoadMessageHandlers()
    {
      return this.ConfigureMessageHandlersIn(NServiceBus.Configure.TypesToScan);
    }

    MyConfigUnicastBus ConfigureMessageHandlersIn(IEnumerable<Type> types)
    {
      var handlers = new List<Type>();
      foreach (Type t in types)
      {
        if (ConfigUnicastBus.IsMessageHandler(t))
        {
          this.Configurer.ConfigureComponent(t, ComponentCallModelEnum.Singlecall);
          handlers.Add(t);
        }
      }
      _config.ConfigureProperty(b => b.MessageHandlerTypes, handlers);
      return this;
    }
  }

  public class MyConfigMsmqSubscriptionStorage : Configure
  {
    private IComponentConfig<MsmqSubscriptionStorage> _config;

    public void Configure(Configure config)
    {
      this.Builder = config.Builder;
      this.Configurer = config.Configurer;

      _config = Configurer.ConfigureComponent<MsmqSubscriptionStorage>(ComponentCallModelEnum.Singleton);
    }

    public MyConfigMsmqSubscriptionStorage In(EndpointAddress address)
    {
      _config.ConfigureProperty(t => t.Queue, address.ToNsbAddress());
      return this;
    }
  }
}
