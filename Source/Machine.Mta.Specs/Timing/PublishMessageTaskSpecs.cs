using System;
using System.Collections.Generic;

using Machine.Mta.InterfacesAsMessages;
using Machine.Mta.Timing;
using Machine.Specifications;

using Rhino.Mocks;

namespace Machine.Mta.Specs.Timing.PublishMessageTaskSpecs
{
  public interface ISimpleMessage : IMessage
  {
  }

  public class with_publish_message_task
  {
    protected static PublishMessageTask<ISimpleMessage> task;
    protected static IMessageBus bus;
    protected static IMessageFactory messageFactory;
    protected static ITrigger trigger;
    protected static ISimpleMessage message;

    Establish context = () =>
    {
      bus = MockRepository.GenerateMock<IMessageBus>();
      messageFactory = MockRepository.GenerateMock<IMessageFactory>();
      trigger = MockRepository.GenerateMock<ITrigger>();
      task = new PublishMessageTask<ISimpleMessage>(bus, messageFactory, trigger);
      message = MockRepository.GenerateMock<ISimpleMessage>();
      messageFactory.Stub(x => x.Create<ISimpleMessage>()).Return(message);
    };
  }

  [Subject("Publish message task")]
  public class when_running_task_and_is_not_fired : with_publish_message_task
  {
    Establish context = () =>
      trigger.Stub(x => x.IsFired()).Return(false);

    Because of = () =>
      task.OnceASecond();

    It should_not_publish_any_messages = () =>
      bus.AssertWasNotCalled(x => x.Publish(message));
  }

  [Subject("Publish message task")]
  public class when_running_task_and_is_fired : with_publish_message_task
  {
    Establish context = () =>
      trigger.Stub(x => x.IsFired()).Return(true);

    Because of = () =>
      task.OnceASecond();

    It should_publish_any_messages = () =>
      bus.AssertWasCalled(x => x.Publish(message));
  }
}
