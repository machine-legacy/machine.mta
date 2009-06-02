using System;

namespace Machine.Mta.Timing
{
  public interface ISchedulePublishMessage : IMessage
  {
    DateTime PublishAt { get; set; }
    EndpointAddress[] PublishAddresses { get; set; }
    MessagePayload MessagePayload { get; set; }
  }
}