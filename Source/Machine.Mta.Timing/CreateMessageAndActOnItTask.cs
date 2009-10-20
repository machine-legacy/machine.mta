using System;
using System.Collections.Generic;

namespace Machine.Mta.Timing
{
  public class CreateMessageAndActOnItTask<T> : IOnceASecondTask where T : class, IMessage
  {
    readonly IMessageBus _bus;
    readonly IMessageFactory _factory;
    readonly ITrigger _trigger;
    readonly Func<IMessageFactory, T> _createMessage;
    readonly Action<IMessageBus, T> _actOnMessage;

    public CreateMessageAndActOnItTask(IMessageBus bus, IMessageFactory factory, ITrigger trigger, Func<IMessageFactory, T> createMessage, Action<IMessageBus, T> actOnMessage)
    {
      _bus = bus;
      _actOnMessage = actOnMessage;
      _createMessage = createMessage;
      _factory = factory;
      _trigger = trigger;
    }

    public void OnceASecond()
    {
      if (_trigger.IsFired())
      {
        _actOnMessage(_bus, _createMessage(_factory));
      }
    }
  }
}
