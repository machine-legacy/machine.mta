using System;
using System.Collections.Generic;

namespace Machine.Mta.Timeouts
{
  public interface ITimeoutClient
  {
    void ScheduleTimeout(Guid id, DateTime timeoutAt);
  }
  public class TimeoutClient : ITimeoutClient
  {
    readonly IMessageBus _bus;
    readonly IMessageFactory _messageFactory;

    public TimeoutClient(IMessageBus bus, IMessageFactory messageFactory)
    {
      _bus = bus;
      _messageFactory = messageFactory;
    }

    public void ScheduleTimeout(Guid id, DateTime timeoutAt)
    {
      IScheduleTimeoutMessage message = _messageFactory.Create<IScheduleTimeoutMessage>();
      message.CorrelationId = id;
      message.TimeoutAt = timeoutAt;
      _bus.Send(message);
    }
  }
}
