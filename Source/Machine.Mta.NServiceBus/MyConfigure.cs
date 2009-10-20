using System;
using System.Collections.Generic;
using System.Linq;

using Machine.Mta.Serializing.Xml;

using NServiceBus;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast;
using NServiceBus.Unicast.Config;
using NServiceBus.Unicast.Subscriptions.Msmq;
using NServiceBus.Unicast.Transport.Msmq;

namespace Machine.Mta
{
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

    public static Configure XmlSerializer(this Configure config)
    {
      var messageTypes = Configure.TypesToScan.Where(t => typeof(NServiceBus.IMessage).IsAssignableFrom(t)).ToList();
      config.Configurer.ConfigureComponent<XmlMessageSerializer>(ComponentCallModelEnum.Singleton).
                        ConfigureProperty(serializer => serializer.MessageTypes, messageTypes);
      return config;
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
      if (listenAddress != EndpointAddress.Null)
        _config.ConfigureProperty(t => t.InputQueue, listenAddress.ToNsbAddress());
      if (poisonAddress != EndpointAddress.Null)
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
            _messageOwners[key] = owner.ToString();
          }
          else
          {
            _messageOwners[key] = String.Empty;
          }
        }
      }
      foreach (var entry in _messageOwners)
      {
        _log.Debug("Configured: " + entry.Key + " to " + entry.Value);
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