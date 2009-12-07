using System;
using System.Collections.Generic;
using NServiceBus;

namespace Machine.Mta.Timing
{
  public class SendLocalEmptyMessageTask<T> : CreateMessageAndActOnItTask<T> where T : class, IMessage
  {
    public SendLocalEmptyMessageTask(IMessageBus bus, IMessageFactory messageFactory, ITrigger trigger)
      : base(bus, messageFactory, trigger, x => x.Create<T>(), (b, m) => b.SendLocal(m))
    {
    }
  }
}
