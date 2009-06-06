using System;
using System.Collections.Generic;
using System.Linq;

using Machine.Container.Services;
using Machine.Core;
using Machine.Mta.Config;

using NServiceBus;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.ObjectBuilder;
using NServiceBus.Saga;
using NServiceBus.Unicast;
using NServiceBus.Unicast.Config;
using NServiceBus.Unicast.Subscriptions.Msmq;
using NServiceBus.Unicast.Transport.Msmq;
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

    public NsbBus Create(EndpointAddress subscriptionStorageAddress, EndpointAddress listenAddress, EndpointAddress poisonAddress, IEnumerable<Type> additionalTypes)
    {
      var types =       _container.Handlers().
                  Union(_container.Finders()).
                  Union(_container.Sagas()).
                  Union(_messageRegisterer.MessageTypes).
                  Union(additionalTypes).ToList();
      return Add(listenAddress, poisonAddress, Configure
        .With(types)
        .MachineBuilder(_container)
        .MsmqSubscriptionStorage()
          .In(subscriptionStorageAddress)
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

    public static Configure StaticSubscriptionStorage(this Configure config)
    {
      var cfg = new MyConfigStaticSubscriptionStorage();
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
    private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(MyConfigUnicastBus));
    private readonly Dictionary<string, string> _messageOwners = new Dictionary<string, string>();
    private IComponentConfig<UnicastBus> _config;
 
    public void Configure(Configure config)
    {
      this.Builder = config.Builder;
      this.Configurer = config.Configurer;

      _config = Configurer.ConfigureComponent<UnicastBus>(ComponentCallModelEnum.Singleton);
      _config.ConfigureProperty(b => b.ForwardReceivedMessagesTo, null);
      _config.ConfigureProperty(b => b.DistributorControlAddress, null);
      _config.ConfigureProperty(b => b.DistributorDataAddress, null);
      _config.ConfigureProperty(b => b.ImpersonateSender, false);
    }

    public MyConfigUnicastBus WithMessageRoutes(IMessageRouting routing)
    {
      foreach (Type type in NServiceBus.Configure.TypesToScan.Union(routing.MessageTypes()))
      {
        if (typeof(NServiceBus.IMessage).IsAssignableFrom(type))
        {
          var key = type.FullName + ", " + type.Assembly.GetName().Name;
          var owner = routing.Owner(type);
          if (owner != null)
          {
            _messageOwners[key] = owner.ToNsbAddress();
          }
          else
          {
            _messageOwners[key] = String.Empty;
          }
        }
      }
      foreach (var entry in _messageOwners)
      {
        _log.Info("Configured: " + entry.Key + " to " + entry.Value);
      }
      _config.ConfigureProperty(b => b.MessageOwners, _messageOwners);

      return this;
    }

    public MyConfigUnicastBus LoadMessageHandlers()
    {
      return ConfigureMessageHandlersIn(NServiceBus.Configure.TypesToScan);
    }

    public MyConfigUnicastBus LoadMessageHandlers<T>(First<T> order)
    {
      var types = new List<Type>(NServiceBus.Configure.TypesToScan);
      foreach (var type in order.Types)
      {
        types.Remove(type);
      }
      types.InsertRange(0, order.Types);
      return ConfigureMessageHandlersIn(types);
    }

    MyConfigUnicastBus ConfigureMessageHandlersIn(IEnumerable<Type> types)
    {
      var handlers = new List<Type>();
      foreach (var type in types)
      {
        if (ConfigUnicastBus.IsMessageHandler(type))
        {
          Configurer.ConfigureComponent(type, ComponentCallModelEnum.Singlecall);
          handlers.Add(type);
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

  public class MyConfigStaticSubscriptionStorage : Configure
  {
    private IComponentConfig<StaticSubscriptionStorage> _config;

    public void Configure(Configure config)
    {
      this.Builder = config.Builder;
      this.Configurer = config.Configurer;

      _config = Configurer.ConfigureComponent<StaticSubscriptionStorage>(ComponentCallModelEnum.Singleton);
    }
  }
}
