using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using log4net.Appender;
using Machine.Container;
using Machine.Container.Plugins.Disposition;
using Machine.Core;
using Machine.Mta.Config;

using NServiceBus;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.Sagas.Impl;
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

    public NsbBus Create(MsmqProperties properties)
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
      return Add(properties.ListenAddress, properties.PoisonAddress, configure.CreateBus());
    }

    public NsbBus Create(AmqpProperties properties)
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
          .On(properties.ListenAddress, properties.PoisonAddress)
        .Sagas()
        .UnicastBus()
          .LoadMessageHandlers(First<GridInterceptingMessageHandler>.Then<SagaMessageHandler>())
          .WithMessageRoutes(_messageDestinations);
      return Add(properties.ListenAddress, EndpointAddress.Null, configure.CreateBus());
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
    NsbBus Create(MsmqProperties properties);
    NsbBus Create(AmqpProperties properties);
    void EachBus(Action<IStartableBus> action);
    void EachBus(Action<NsbBus> action);
    NsbBus CurrentBus();
  }

  public class Fun
  {
    public void Run()
    {
      var loggingConfiguration = new NameValueCollection();
      loggingConfiguration["configType"] = "EXTERNAL";
      Common.Logging.LogManager.Adapter = new Common.Logging.Log4Net.Log4NetLoggerFactoryAdapter(loggingConfiguration);
      log4net.Config.BasicConfigurator.Configure(new OutputDebugStringAppender() { Layout = new log4net.Layout.PatternLayout("%-5p (%30.30c) %m%n") });

      var container = new MachineContainer();
      container.Initialize();
      container.AddPlugin(new DisposablePlugin());
      container.PrepareForServices();
      container.Register.Type<MessageDestinations>();
      container.Register.Type<MessageRegisterer>();
      container.Register.Type<NsbMessageBusFactory>();
      container.Register.Type<NsbMessageFactory>();
      container.Register.Type<HelloHandler>();
      container.Register.Type<MessageBusProxy>();
      container.Register.Type<NsbMessageBusManager>();
      container.Start();
      var registerer = container.Resolve.Object<IMessageRegisterer>();
      registerer.AddMessageTypes(new[] { typeof(IHelloMessage) });
      var messageFactory = container.Resolve.Object<IMessageFactory>();
      var factory = container.Resolve.Object<NsbMessageBusFactory>();
      var bus = factory.Create(new AmqpProperties() {
        ListenAddress = EndpointAddress.FromString("amqp://192.168.0.173//www/test1"),
        PoisonAddress = EndpointAddress.FromString("amqp://192.168.0.173//www/test1p")
      });
      bus.Start();
      bus.Bus.Send("amqp://192.168.0.173//www/test1", messageFactory.Create<IHelloMessage>(m => { m.Name = "Andy"; m.Age = 1; }));
      bus.Bus.Send("amqp://192.168.0.173//www/test1", messageFactory.Create<IHelloMessage>(m => { m.Name = ""; m.Age = 0; }));
      bus.Bus.Send("amqp://192.168.0.173//www/test1", messageFactory.Create<IHelloMessage>(m => { m.Name = "Jacob"; m.Age = 0; }));
      System.Threading.Thread.Sleep(TimeSpan.FromSeconds(6));
      container.Dispose();
      System.Threading.Thread.Sleep(TimeSpan.FromSeconds(2));
    }
  }

  public interface IHelloMessage : IMessage
  {
    string Name { get; set; }
    Int32 Age { get; set; }
  }

  public class HelloHandler : IMessageHandler<IHelloMessage>
  {
    readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof (HelloHandler));
    readonly IMessageBus _bus;
    readonly IMessageFactory _messageFactory;

    public HelloHandler(IMessageBus bus, IMessageFactory messageFactory)
    {
      _bus = bus;
      _messageFactory = messageFactory;
    }

    public void Handle(IHelloMessage message)
    {
      if (String.IsNullOrEmpty(message.Name)) throw new ArgumentException();
      _log.Info("Hello " + message.Name + ": " + message.Age);
      if (message.Age > 0)
      {
        _bus.Reply(_messageFactory.Create<IHelloMessage>(m => { m.Name = message.Name; m.Age = message.Age - 1; }));
      }
    }
  }

  public class MsmqProperties
  {
    public EndpointAddress ListenAddress { get; set; }
    public EndpointAddress PoisonAddress { get; set; }
    public IEnumerable<Type> AdditionalTypes { get; set; }

    public MsmqProperties()
    {
      this.ListenAddress = EndpointAddress.Null;
      this.PoisonAddress = EndpointAddress.Null;
      this.AdditionalTypes = new Type[0];
    }
  }

  public class AmqpProperties
  {
    public EndpointAddress ListenAddress { get; set; }
    public EndpointAddress PoisonAddress { get; set; }
    public IEnumerable<Type> AdditionalTypes { get; set; }

    public AmqpProperties()
    {
      this.ListenAddress = EndpointAddress.Null;
      this.PoisonAddress = EndpointAddress.Null;
      this.AdditionalTypes = new Type[0];
    }
  }
}
