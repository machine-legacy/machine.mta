using System;

namespace Machine.Mta.Timing
{
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
}