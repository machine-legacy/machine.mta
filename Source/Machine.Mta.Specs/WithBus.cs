using System;
using System.Collections.Generic;

using Machine.Container;
using Machine.Container.Services;
using Machine.Mta.Dispatching;
using Machine.Mta.InterfacesAsMessages;
using Machine.Mta.Transports.Msmq;
using Machine.Mta.Endpoints;
using Machine.Mta.Sagas;
using Machine.Specifications;
using Machine.Utility.ThreadPool;

using Rhino.Mocks;

namespace Machine.Mta.Specs
{
  public interface ISampleMessage : IMessage
  {
  }

  public interface ISampleSagaMessage : ISagaMessage
  {
  }

  [Subject("Message dispatching")]
  public class when_dispatching_a_message_with_no_handlers : DispatchSpecs
  {
    static Exception error;

    Because of = () =>
      error = Catch.Exception(() => dispatcher.Dispatch(new IMessage[] { message2 }));

    It should_return_with_no_exceptions = () =>
      error.ShouldBeNull();
  }

  [Subject("Message dispatching")]
  public class when_dispatching_a_message : DispatchSpecs
  {
    static IConsume<ISampleMessage> handler;

    Establish context = () =>
    {
      handler = MockRepository.GenerateMock<IConsume<ISampleMessage>>();
      container.Register.Type<IConsume<ISampleMessage>>().Is(handler);
    };

    Because of = () =>
      dispatcher.Dispatch(new IMessage[] { message2 });

    It should_call_the_handler = () =>
      handler.AssertWasCalled(x => x.Consume(message2));
  }

  [Subject("Message dispatching")]
  public class when_dispatching_a_message_with_two_applicable_handlers : DispatchSpecs
  {
    static IConsume<IMessage> handler1;
    static IConsume<ISampleMessage> handler2;

    Establish context = () =>
    {
      handler1 = MockRepository.GenerateMock<IConsume<IMessage>>();
      handler2 = MockRepository.GenerateMock<IConsume<ISampleMessage>>();
      container.Register.Type<IConsume<IMessage>>().Is(handler1);
      container.Register.Type<IConsume<ISampleMessage>>().Is(handler2);
    };

    Because of = () =>
      dispatcher.Dispatch(new IMessage[] { message2 });

    It should_call_the_first_handler = () =>
      handler1.AssertWasCalled(x => x.Consume(message2));

    It should_call_the_second_handler = () =>
      handler2.AssertWasCalled(x => x.Consume(message2));
  }

  [Subject("Message dispatching")]
  public class when_dispatching_a_message_with_inapplicable_handlers : DispatchSpecs
  {
    static IConsume<IMessage> handler1;
    static IConsume<ISampleMessage> handler2;
    static IConsume<ISampleSagaMessage> handler3;

    Establish context = () =>
    {
      handler1 = MockRepository.GenerateMock<IConsume<IMessage>>();
      handler2 = MockRepository.GenerateMock<IConsume<ISampleMessage>>();
      handler3 = MockRepository.GenerateMock<IConsume<ISampleSagaMessage>>();
      container.Register.Type<IConsume<IMessage>>().Is(handler1);
      container.Register.Type<IConsume<ISampleMessage>>().Is(handler2);
      container.Register.Type<IConsume<ISampleSagaMessage>>().Is(handler3);
    };

    Because of = () =>
      dispatcher.Dispatch(new IMessage[] { message2 });

    It should_call_the_first_handler = () =>
      handler1.AssertWasCalled(x => x.Consume(message2));

    It should_call_the_second_handler = () =>
      handler2.AssertWasCalled(x => x.Consume(message2));
  }

  public interface IConsumeMessageAndSampleMessage : IConsume<IMessage>, IConsume<ISampleMessage>
  {
  }

  [Subject("Message dispatching")]
  public class when_dispatching_a_message_two_handler_with_two_applicable_consumers : DispatchSpecs
  {
    static IConsumeMessageAndSampleMessage handler;

    Establish context = () =>
    {
      handler = MockRepository.GenerateMock<IConsumeMessageAndSampleMessage>();
      container.Register.Type<IConsumeMessageAndSampleMessage>().Is(handler);
    };

    Because of = () =>
      dispatcher.Dispatch(new IMessage[] { message2 });

    It should_call_specific_handler = () =>
      handler.AssertWasCalled(x => x.Consume(message2));

    It should_not_call_general_handler = () =>
      handler.AssertWasNotCalled(x => x.Consume((IMessage)message2));
  }

  public class BusSpecs
  {
    protected static MessageBus bus;
    protected static MessageDispatcher dispatcher;
    protected static EndpointAddress listeningOnAddress = NameAndHostAddress.ForLocalQueue("test").ToAddress();
    protected static EndpointAddress poisonAddress = NameAndHostAddress.ForLocalQueue("error").ToAddress();
    protected static IMessageFactory messageFactory;
    protected static IMessage message1;
    protected static ISampleMessage message2;
    protected static ISampleSagaMessage message3;
    protected static IMachineContainer container;
    protected static IMessageDestinations messageDestinations;

    Establish context = () =>
    {
      container = new MachineContainer();
      container.Initialize();
      container.PrepareForServices();
      container.Register.Type<SagaAspect>();
      container.Register.Type<MsmqEndpointFactory>();
      container.Register.Type<MsmqTransactionManager>();
      container.Register.Type<TransactionManager>();
      container.Start();
      IEndpointResolver endpointResolver = new EndpointResolver(container);
      messageDestinations = new MessageDestinations();
      MessageRegisterer registerer = new MessageRegisterer();
      registerer.AddMessageTypes(typeof(IMessage), typeof(ISampleMessage), typeof(ISampleSagaMessage));
      MessageInterfaceImplementations messageInterfaceImplementations = new MessageInterfaceImplementations(new DefaultMessageInterfaceImplementationFactory(), registerer);
      TransportMessageBodySerializer transportMessageBodySerializer = new TransportMessageBodySerializer(new MessageInterfaceTransportFormatter(messageInterfaceImplementations));
      dispatcher = new MessageDispatcher(container, new DefaultMessageAspectsProvider(container), new AllHandlersInContainer(container));
      messageFactory = new MessageFactory(messageInterfaceImplementations, new MessageDefinitionFactory());
      bus = new MessageBus(endpointResolver, messageDestinations, transportMessageBodySerializer, dispatcher, listeningOnAddress, poisonAddress, new TransactionManager(), new MessageFailureManager(new FailureConfiguration(1)), ThreadPoolConfiguration.OneAndOne);
      message1 = messageFactory.Create<IMessage>();
      message2 = messageFactory.Create<ISampleMessage>();
      message3 = messageFactory.Create<ISampleSagaMessage>();
    };
  }

  public class DispatchSpecs : BusSpecs
  {
    Establish context = () =>
    {
      CurrentMessageContext.Open(TransportMessage.For(EndpointAddress.Null, null, new Guid[0], new MessagePayload(new byte[0], "NULL")));
    };
  }
}
