using System;
using NServiceBus;

namespace Machine.Mta.Timing
{
  public interface ISchedulePublishMessage : IMessage
  {
    DateTime PublishAt { get; set; }
    string[] PublishAddresses { get; set; }
    byte[] MessagePayload { get; set; }
  }
}