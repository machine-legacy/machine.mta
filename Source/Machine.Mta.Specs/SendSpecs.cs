using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Transactions;
using log4net.Appender;
using Machine.Container;
using Machine.Container.Plugins.Disposition;
using Machine.Mta.MessageInterfaces;
using NServiceBus;

namespace Machine.Mta.Specs
{
  public class SendSpecs
  {
    public void Run()
    {
      var loggingConfiguration = new NameValueCollection();
      loggingConfiguration["configType"] = "EXTERNAL";
      Common.Logging.LogManager.Adapter = new Common.Logging.Log4Net.Log4NetLoggerFactoryAdapter(loggingConfiguration);
      log4net.Config.BasicConfigurator.Configure(new OutputDebugStringAppender() { Layout = new log4net.Layout.PatternLayout("SPECIAL %-5p [%-20thread] (%30.30c) %m%n") });

      var container = new MachineContainer();
      container.Initialize();
      container.AddPlugin(new DisposablePlugin());
      container.PrepareForServices();
      container.Register.Type<MessageDestinations>();
      container.Register.Type<MessageRegisterer>();
      container.Register.Type<NsbMessageBusFactory>();
      container.Register.Type<HelloHandler>();
      container.Register.Type<MessageBusProxy>();
      container.Register.Type<NsbMessageBusManager>();

      container.Register.Type<NsbMessageFactory>();
      // container.Register.Type<MessageFactory>();
      // container.Register.Type<MessageInterfaceImplementations>();
      // container.Register.Type<DefaultMessageInterfaceImplementationFactory>();
      // container.Register.Type<MessageDefinitionFactory>();
      container.Start();
      var routing = container.Resolve.Object<IMessageRouting>();
      routing.AssignOwner<IHelloMessage>(EndpointAddress.FromString("amqp://192.168.0.173//el.www/test1"));
      var registerer = container.Resolve.Object<IMessageRegisterer>();
      registerer.AddMessageTypes(new[] { typeof(IHelloMessage) });
      var messageFactory = container.Resolve.Object<IMessageFactory>();
      var factory = container.Resolve.Object<NsbMessageBusFactory>();
      var bus = factory.Create(new BusProperties() {
        ListenAddress = EndpointAddress.FromString("amqp://192.168.0.173//el.www/el.www.test1"),
        PoisonAddress = EndpointAddress.FromString("amqp://192.168.0.173//el.www/el.www.test1.poison"),
        TransportType = TransportType.RabbitMq
      });
      bus.Start();
      System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1));
      using (var scope = new TransactionScope())
      {
        bus.Bus.Send("amqp://192.168.0.173//el.www/el.www.test1", messageFactory.Create<IHelloMessage>(m => { m.Name = "Andy"; m.Age = 1; }));
        bus.Bus.Send("amqp://192.168.0.173//el.www/el.www.test1", messageFactory.Create<IHelloMessage>(m => { m.Name = ""; m.Age = 0; }));
        bus.Bus.Send("amqp://192.168.0.173//el.www/el.www.test1", messageFactory.Create<IHelloMessage>(m => { m.Name = "Jacob"; m.Age = 0; }));
        scope.Complete();
      }
      System.Threading.Thread.Sleep(TimeSpan.FromSeconds(6));
      container.Dispose();
      System.Threading.Thread.Sleep(TimeSpan.FromSeconds(2));
    }
  }

  public interface IHelloMessage : IMessage, NServiceBus.IMessage
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
      if (String.IsNullOrEmpty(message.Name)) return; // throw new ArgumentException();
      _log.Info("Hello " + message.Name + ": " + message.Age);
      if (message.Age > 0)
      {
        _bus.Request(_messageFactory.Create<IHelloMessage>(m => { m.Name = message.Name; m.Age = -1; })).OnReply(ar => {
          _log.Info("********* BACK");
        });
      }
      else if (message.Age == -1)
      {
        _bus.Reply(_messageFactory.Create<IHelloMessage>(m => { m.Name = message.Name; m.Age = 0; }));
      }
    }
  }
}
