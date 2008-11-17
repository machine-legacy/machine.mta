using System;
using System.Collections.Generic;

using MassTransit.ServiceBus;
using MassTransit.ServiceBus.Services.Timeout;

namespace Machine.Mta.Timeouts
{
  public class TimeoutHandlers : Consumes<IScheduleTimeoutMessage>.All
  {
    readonly ITimeoutRepository _timeoutRepository;

    public TimeoutHandlers(ITimeoutRepository timeoutRepository)
    {
      _timeoutRepository = timeoutRepository;
    }

    public void Consume(IScheduleTimeoutMessage message)
    {
      _timeoutRepository.Schedule(message.CorrelationId, message.TimeoutAt);
    }
  }
}