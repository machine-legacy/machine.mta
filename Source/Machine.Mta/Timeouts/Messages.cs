using System;
using System.Collections.Generic;

namespace Machine.Mta.Timeouts
{
  public interface IScheduleTimeoutMessage : IMessage
  {
    Guid CorrelationId { get; set; }
    DateTime TimeoutAt { get; set; }
  }
  public interface ITimeoutExpiredMessage : IMessage
  {
    Guid CorrelationId { get; set; }
  }
}
