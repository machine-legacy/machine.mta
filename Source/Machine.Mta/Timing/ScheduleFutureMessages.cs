using System;
using System.Collections.Generic;
using System.Linq;

using MassTransit.ServiceBus;

using Machine.Mta.Minimalistic;

namespace Machine.Mta.Timing
{
  public interface ISchedulePublishMessage : IMessage
  {
    DateTime PublishAt { get; set; }
    EndpointName[] PublishAddresses { get; set; }
    MessagePayload MessagePayload { get; set; }
  }
  public interface IScheduleFutureMessages
  {
    void PublishAt<T>(DateTime publishAt, params T[] messages) where T : class, IMessage;
  }
  public class ScheduleFutureMessages : IScheduleFutureMessages
  {
    readonly IMessageFactory _messageFactory;
    readonly IMessageBus _bus;
    readonly IMessageEndpointLookup _messageEndpointLookup;
    readonly TransportMessageBodySerializer _transportMessageBodySerializer;

    public ScheduleFutureMessages(IMessageFactory messageFactory, IMessageBus bus, IMessageEndpointLookup messageEndpointLookup, TransportMessageBodySerializer transportMessageBodySerializer)
    {
      _messageFactory = messageFactory;
      _messageEndpointLookup = messageEndpointLookup;
      _transportMessageBodySerializer = transportMessageBodySerializer;
      _bus = bus;
    }

    public void PublishAt<T>(DateTime publishAt, params T[] messages) where T : class, IMessage
    {
      ICollection<EndpointName> destinations = _messageEndpointLookup.LookupEndpointsFor(typeof(T));
      ISchedulePublishMessage message = _messageFactory.Create<ISchedulePublishMessage>();
      message.PublishAddresses = destinations.ToArray();
      message.PublishAt = publishAt;
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
      ScheduledPublish scheduled = new ScheduledPublish(message.PublishAt, message.MessagePayload, message.PublishAddresses);
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
        foreach (EndpointName destination in scheduled.Addresses)
        {
          _bus.Send(destination, scheduled.MessagePayload);
        }
      }
    }
  }
  public class ScheduledPublish
  {
    readonly DateTime _publishAt;
    readonly MessagePayload _messagePayload;
    readonly EndpointName[] _addresses;

    public DateTime PublishAt
    {
      get { return _publishAt; }
    }

    public MessagePayload MessagePayload
    {
      get { return _messagePayload; }
    }

    public EndpointName[] Addresses
    {
      get { return _addresses; }
    }

    public ScheduledPublish(DateTime publishAt, MessagePayload messagePayload, EndpointName[] addresses)
    {
      _publishAt = publishAt;
      _messagePayload = messagePayload;
      _addresses = addresses;
    }
  }
}
