namespace Machine.Mta.Timing
{
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
