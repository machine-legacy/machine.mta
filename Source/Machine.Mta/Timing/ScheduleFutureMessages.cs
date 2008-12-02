using System;
using System.Collections.Generic;

using MassTransit.ServiceBus;

using Machine.Mta.Minimalistic;

namespace Machine.Mta.Timing
{
  public interface ISchedulePublishMessage : IMessage
  {
    DateTime PublishAt { get; set; }
    MessagePayload MessagePayload { get; set; }
  }
  public interface IScheduleFutureMessages
  {
    void PublishAt(DateTime at, params IMessage[] messages);
  }
  public class ScheduleFutureMessages : IScheduleFutureMessages
  {
    private readonly IMessageFactory _messageFactory;
    private readonly IMessageBus _bus;
    private readonly TransportMessageBodySerializer _transportMessageBodySerializer;

    public ScheduleFutureMessages(IMessageFactory messageFactory, IMessageBus bus, TransportMessageBodySerializer transportMessageBodySerializer)
    {
      _messageFactory = messageFactory;
      _transportMessageBodySerializer = transportMessageBodySerializer;
      _bus = bus;
    }

    public void PublishAt(DateTime at, params IMessage[] messages)
    {
      ISchedulePublishMessage message = _messageFactory.Create<ISchedulePublishMessage>();
      message.PublishAt = at;
      message.MessagePayload = new MessagePayload(_transportMessageBodySerializer.Serialize(messages));
      _bus.Send(message);
    }
  }
  public class SchedulePublishHandler : Consumes<ISchedulePublishMessage>.All
  {
    readonly IScheduledPublishRepository _scheduledPublishRepository;

    public SchedulePublishHandler(IScheduledPublishRepository scheduledPublishRepository)
    {
      _scheduledPublishRepository = scheduledPublishRepository;
    }

    public void Consume(ISchedulePublishMessage message)
    {
      EndpointName sender = CurrentMessageContext.Current.TransportMessage.ReturnAddress;
      ScheduledPublish scheduled = new ScheduledPublish(message.PublishAt, message.MessagePayload, sender);
      _scheduledPublishRepository.Add(scheduled);
    }
  }
  public interface IScheduledPublishRepository
  {
    void Add(ScheduledPublish scheduled);
    ICollection<ScheduledPublish> FindAllExpired();
  }
  public class InMemoryScheduledPublishRepository : IScheduledPublishRepository
  {
    readonly List<ScheduledPublish> _scheduled = new List<ScheduledPublish>();
    readonly object _lock = new object();

    public void Add(ScheduledPublish scheduled)
    {
      lock (_lock)
      {
        _scheduled.Add(scheduled);
      }
    }

    public ICollection<ScheduledPublish> FindAllExpired()
    {
      lock (_lock)
      {
        DateTime now = ServerClock.Now();
        List<ScheduledPublish> expired = new List<ScheduledPublish>();
        foreach (ScheduledPublish schedule in new List<ScheduledPublish>(_scheduled))
        {
          if (schedule.PublishAt < now)
          {
            expired.Add(schedule);
            _scheduled.Remove(schedule);
          }
        }
        return expired;
      }
    }
  }
  public class PublishScheduledMessagesTask : IOnceASecondTask
  {
    readonly IScheduledPublishRepository _scheduledPublishRepository;
    readonly IMessageBus _bus;

    public PublishScheduledMessagesTask(IMessageBus bus, IScheduledPublishRepository scheduledPublishRepository)
    {
      _bus = bus;
      _scheduledPublishRepository = scheduledPublishRepository;
    }

    public void OnceASecond()
    {
      foreach (ScheduledPublish scheduled in _scheduledPublishRepository.FindAllExpired())
      {
        _bus.Send(scheduled.ReturnAddress, scheduled.MessagePayload);
      }
    }
  }
  public class ScheduledPublish
  {
    readonly DateTime _publishAt;
    readonly MessagePayload _messagePayload;
    readonly EndpointName _returnAddress;

    public DateTime PublishAt
    {
      get { return _publishAt; }
    }

    public MessagePayload MessagePayload
    {
      get { return _messagePayload; }
    }

    public EndpointName ReturnAddress
    {
      get { return _returnAddress; }
    }

    public ScheduledPublish(DateTime publishAt, MessagePayload messagePayload, EndpointName returnAddress)
    {
      _publishAt = publishAt;
      _messagePayload = messagePayload;
      _returnAddress = returnAddress;
    }
  }
}
