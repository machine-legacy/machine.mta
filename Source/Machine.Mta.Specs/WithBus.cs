using System;
using System.Collections.Generic;

using MassTransit.ServiceBus;
using MassTransit.ServiceBus.Internal;
using MassTransit.ServiceBus.MSMQ;

using Machine.Mta.Sagas;
using Machine.Container;
using Machine.Container.Services;
using Machine.Mta.InterfacesAsMessages;
using Machine.Mta.Minimalistic;

using Machine.Specifications;
using Rhino.Mocks;

namespace Machine.Mta.Specs
{
  public class with_bus
  {
    protected static MessageBus bus;
    protected static MessageDispatcher dispatcher;
    protected static EndpointName listeningOnName = EndpointName.ForLocalQueue("test");
    protected static EndpointName poisonName = EndpointName.ForLocalQueue("error");
    protected static IMessageFactory messageFactory;
    protected static IMessage message1;
    protected static ISampleMessage message2;
    protected static IMachineContainer container;

    Establish context = () =>
    {
      EndpointResolver.AddTransport(typeof(MsmqEndpoint));
      container = new MachineContainer();
      container.Initialize();
      container.PrepareForServices();
      container.Register.Type<SagaAspect>();
      container.Start();  
      IEndpointResolver endpointResolver = new EndpointResolver();
      IMtaUriFactory uriFactory = new MsMqUriFactory();
      IMessageEndpointLookup messageEndpointLookup = new MessageEndpointLookup();
      MessageInterfaceImplementations messageInterfaceImplementations = new MessageInterfaceImplementations();
      messageInterfaceImplementations.GenerateImplementationsOf(typeof(IMessage), typeof(ISampleMessage), typeof(ISampleSagaMessage));
      TransportMessageBodySerializer transportMessageBodySerializer = new TransportMessageBodySerializer(new MessageInterfaceTransportFormatter(messageInterfaceImplementations));
      dispatcher = new MessageDispatcher(container, new DefaultMessageAspectsProvider(container));
      messageFactory = new MessageFactory(messageInterfaceImplementations);
      bus = new MessageBus(endpointResolver, uriFactory, messageEndpointLookup, transportMessageBodySerializer, dispatcher, listeningOnName, poisonName);
      message1 = messageFactory.Create<IMessage>();
      message2 = messageFactory.Create<ISampleMessage>();
    };
  }

  public interface ISampleMessage : IMessage
  {
  }

  public interface ISampleSagaMessage : ISagaMessage
  {
  }

  [Subject("Message dispatching")]
  public class when_dispatching_a_message_with_no_handlers : with_bus
  {
    static Exception error;

    Because of = () =>
      error = Catch.Exception(() => dispatcher.Dispatch(new IMessage[] { message2 }));

    It should_return_with_no_exceptions = () =>
      error.ShouldBeNull();
  }

  [Subject("Message dispatching")]
  public class when_dispatching_a_message : with_bus
  {
    static Consumes<ISampleMessage>.All handler;

    Establish context = () =>
    {
      handler = MockRepository.GenerateMock<Consumes<ISampleMessage>.All>();
      container.Register.Type<Consumes<ISampleMessage>.All>().Is(handler);
    };

    Because of = () =>
      dispatcher.Dispatch(new IMessage[] { message2 });

    It should_call_the_handler = () =>
      handler.AssertWasCalled(x => x.Consume(message2));
  }

  [Subject("Message dispatching")]
  public class when_dispatching_a_message_with_two_applicable_handlers : with_bus
  {
    static Consumes<IMessage>.All handler1;
    static Consumes<ISampleMessage>.All handler2;

    Establish context = () =>
    {
      handler1 = MockRepository.GenerateMock<Consumes<IMessage>.All>();
      handler2 = MockRepository.GenerateMock<Consumes<ISampleMessage>.All>();
      container.Register.Type<Consumes<IMessage>.All>().Is(handler1);
      container.Register.Type<Consumes<ISampleMessage>.All>().Is(handler2);
    };

    Because of = () =>
      dispatcher.Dispatch(new IMessage[] { message2 });

    It should_call_the_first_handler = () =>
      handler1.AssertWasCalled(x => x.Consume(message2));

    It should_call_the_second_handler = () =>
      handler2.AssertWasCalled(x => x.Consume(message2));
  }
}
