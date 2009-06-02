using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Machine.Container.Services;
using Machine.Mta.Config;
using Machine.Mta.Dispatching;

using NServiceBus;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast;
using NServiceBus.Unicast.Config;
using NServiceBus.Unicast.Subscriptions.Msmq;
using NServiceBus.Unicast.Transport.Msmq;

namespace Machine.Mta
{
  public interface INsbMessageBusFactory
  {
    IBus Create(EndpointAddress listenAddress, EndpointAddress poisonAddress, IEnumerable<Type> additionalTypes);
    IBus Create(EndpointAddress subscriptionStorageAddress, EndpointAddress listenAddress, EndpointAddress poisonAddress, IEnumerable<Type> additionalTypes);
  }

  public class NsbMessageBusFactory : INsbMessageBusFactory
  {
    readonly IMachineContainer _container;

    public NsbMessageBusFactory(IMachineContainer container)
    {
      _container = container;
    }

    public IBus Create(EndpointAddress listenAddress, EndpointAddress poisonAddress, IEnumerable<Type> additionalTypes)
    {
      return Configure
        .With(_container.Handlers().Union(additionalTypes))
        .MachineBuilder(_container)
        .XmlSerializer()
        .MsmqTransport()
          .On(listenAddress, poisonAddress)
        .UnicastBus()
          .LoadMessageHandlers()
        .CreateBus()
        .Start();
    }

    public IBus Create(EndpointAddress subscriptionStorageAddress, EndpointAddress listenAddress, EndpointAddress poisonAddress, IEnumerable<Type> additionalTypes)
    {
      return Configure
        .With(_container.Handlers().Union(additionalTypes))
        .MachineBuilder(_container)
        .MsmqSubscriptionStorage()
          .In(subscriptionStorageAddress)
        .XmlSerializer()
        .MsmqTransport()
          .On(listenAddress, poisonAddress)
        .UnicastBus()
          .LoadMessageHandlers()
        .CreateBus()
        .Start();
    }
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

  public static class TypesScanner
  {
    public static IEnumerable<Type> MessagesFrom(params Assembly[] messageAssemblies)
    {
      return PossiblyDuplicateMessagesFrom(messageAssemblies).Distinct();
    }

    static IEnumerable<Type> PossiblyDuplicateMessagesFrom(params Assembly[] messageAssemblies)
    {
      foreach (var assembly in messageAssemblies)
      {
        foreach (var type in assembly.GetTypes())
        {
          if (typeof(IMessage).IsAssignableFrom(type))
          {
            yield return type;
          }
        }
      }
    }

    public static IEnumerable<Type> Handlers(this IMachineContainer container)
    {
      foreach (var handlerType in AllMessageHandlerTypes(container))
      {
        if (typeof(NServiceBus.IMessage).IsAssignableFrom(handlerType.TargetExpectsMessageOfType))
        {
          yield return MessageHandlerProxies.For(handlerType.TargetExpectsMessageOfType, handlerType.TargetType);
        }
      }
    }

    static IEnumerable<MessageHandlerType> AllMessageHandlerTypes(IMachineContainer container)
    {
      foreach (var handlerType in new AllHandlersInContainer(container).HandlerTypes())
      {
        var handlerConsumes = handlerType.AllGenericVariations(typeof(IConsume<>));
        foreach (var type in handlerConsumes)
        {
          yield return new MessageHandlerType(handlerType, type);
        }
      }
    }
  }
}
