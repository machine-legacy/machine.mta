using System;
using System.Collections.Generic;

using Machine.Mta.Sagas;

using Machine.Specifications;
using Rhino.Mocks;

namespace Machine.Mta.Specs
{
  public class with_saga : with_bus
  {
    protected static SampleSaga saga;
    protected static ISagaState state;
    protected static ISagaStateRepository<ISagaState> repository;
    protected static ISagaMessage sagaMessage;

    Establish context = () =>
    {
      sagaMessage = messageFactory.Create<ISampleSagaMessage>();
      sagaMessage.SagaId = Guid.NewGuid();
      state = MockRepository.GenerateMock<ISagaState>();
      state.Stub(x => x.SagaId).Return(sagaMessage.SagaId);
      repository = MockRepository.GenerateMock<ISagaStateRepository<ISagaState>>();
      saga = new SampleSaga();
      saga.InitialState = state;
      container.Register.Type<SampleSaga>().Is(saga);
      container.Register.Type<ISagaStateRepository<ISagaState>>().Is(repository);
      CurrentMessageContext.Open(TransportMessage.For(EndpointAddress.Null, Guid.Empty, new Guid[0], new MessagePayload(new byte[0], "NULL")));
    };
  }

  [Subject("Message dispatching with Sagas")]
  public class when_dispatching_non_saga_message_to_saga : with_saga
  {
    Establish context = () =>
    {
      saga.InitialState = null;
    };
    
    Because of = () =>
      dispatcher.Dispatch(new IMessage[] { message2 });

    It should_not_call_the_saga = () =>
      saga.Consumed.ShouldBeEmpty();

    It should_have_null_state = () =>
      saga.State.ShouldBeNull();

    It should_not_save_state = () =>
      repository.AssertWasNotCalled(x => x.Save(state));
  }

  [Subject("Message dispatching with Sagas")]
  public class when_dispatching_non_saga_message_to_saga_that_creates_new_state : with_saga
  {
    Because of = () =>
      dispatcher.Dispatch(new IMessage[] { message3 });

    It should_call_the_saga = () =>
      saga.Consumed.ShouldContainOnly(message3);

    It should_have_initial_state = () =>
      saga.State.ShouldEqual(state);

    It should_save_state = () =>
      repository.AssertWasCalled(x => x.Add(state));
  }

  [Subject("Message dispatching with Sagas")]
  public class when_dispatching_saga_message_to_saga : with_saga
  {
    Establish context = () =>
      repository.Stub(x => x.FindSagaState(sagaMessage.SagaId)).Return(state);

    Because of = () =>
      dispatcher.Dispatch(new IMessage[] { sagaMessage });

    It should_call_the_saga = () =>
      saga.Consumed.ShouldContainOnly(sagaMessage);

    It should_have_initial_state = () =>
      saga.State.ShouldEqual(state);

    It should_save_state = () =>
      repository.AssertWasCalled(x => x.Save(state));
  }

  [Subject("Message dispatching with Sagas")]
  public class when_dispatching_saga_message_to_saga_that_completes_the_saga : with_saga
  {
    Establish context = () =>
    {
      repository.Stub(x => x.FindSagaState(sagaMessage.SagaId)).Return(state);
      state.Stub(x => x.IsSagaComplete).Return(true);
    };

    Because of = () =>
      dispatcher.Dispatch(new IMessage[] { sagaMessage });

    It should_call_the_saga = () =>
      saga.Consumed.ShouldContainOnly(sagaMessage);

    It should_have_initial_state = () =>
      saga.State.ShouldEqual(state);

    It should_delete_state = () =>
      repository.AssertWasCalled(x => x.Delete(state));
  }

  public class SampleSaga : Saga<ISagaState>, IConsume<ISampleMessage>, ISagaStartedBy<ISampleSagaMessage>
  {
    readonly Queue<IMessage> _consumed = new Queue<IMessage>();
    ISagaState _initialState;

    public ISagaState InitialState
    {
      get { return _initialState; }
      set { _initialState = value; }
    }

    public Queue<IMessage> Consumed
    {
      get { return _consumed; }
    }

    public void Consume(ISampleMessage message)
    {
      _consumed.Enqueue(message);
    }

    public void Consume(ISampleSagaMessage message)
    {
      this.State = _initialState;
      _consumed.Enqueue(message);
    }
  }
}

