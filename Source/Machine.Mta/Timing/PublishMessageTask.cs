using System;

using Machine.Mta;
using Machine.Mta.InterfacesAsMessages;

namespace Machine.Mta.Timing
{
  public class PublishMessageTask<T> : IOnceASecondTask where T : class, IMessage
  {
    readonly IMessageBus _bus;
    readonly IMessageFactory _messageFactory;
    readonly ITrigger _trigger;

    public PublishMessageTask(IMessageBus bus, IMessageFactory messageFactory, ITrigger trigger)
    {
      _bus = bus;
      _messageFactory = messageFactory;
      _trigger = trigger;
    }

    public void OnceASecond()
    {
      if (_trigger.IsFired())
      {
        _bus.Publish(_messageFactory.Create<T>());
      }
    }
  }
}