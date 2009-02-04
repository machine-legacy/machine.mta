using System;

namespace Machine.Mta.Timing
{
  public class PublishEmptyMessageTask<T> : PublishFuncMessageTask<T> where T : class, IMessage
  {
    public PublishEmptyMessageTask(IMessageBus bus, IMessageFactory messageFactory, ITrigger trigger)
      : base(bus, messageFactory, trigger, x => x.Create<T>())
    {
    }
  }
  public class PublishFuncMessageTask<T> : PublishMessageTask<T> where T : class, IMessage
  {
    readonly Func<IMessageFactory, T> _ctor;

    public PublishFuncMessageTask(IMessageBus bus, IMessageFactory messageFactory, ITrigger trigger, Func<IMessageFactory, T> ctor)
      : base(bus, messageFactory, trigger)
    {
      _ctor = ctor;
    }

    protected override T CreateMessage()
    {
      return _ctor(this.MessageFactory);
    }
  }
  public abstract class PublishMessageTask<T> : IOnceASecondTask where T : class, IMessage
  {
    readonly IMessageBus _bus;
    readonly IMessageFactory _messageFactory;
    readonly ITrigger _trigger;

    protected IMessageFactory MessageFactory
    {
      get { return _messageFactory; }
    }

    protected IMessageBus Bus
    {
      get { return _bus; }
    }

    protected PublishMessageTask(IMessageBus bus, IMessageFactory messageFactory, ITrigger trigger)
    {
      _bus = bus;
      _messageFactory = messageFactory;
      _trigger = trigger;
    }

    public void OnceASecond()
    {
      if (_trigger.IsFired())
      {
        _bus.Publish(CreateMessage());
      }
    }

    protected virtual T CreateMessage()
    {
      return _messageFactory.Create<T>();
    }
  }
}
