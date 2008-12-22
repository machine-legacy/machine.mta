using System;
using System.Collections.Generic;
using System.Linq;

using MassTransit;

using Machine.Mta.Internal;

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
    void PublishAt<T>(DateTime publishAt, EndpointName destination, params T[] messages) where T : class, IMessage;
    void PublishAt<T>(DateTime publishAt, EndpointName[] destinations, params T[] messages) where T : class, IMessage;
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
      EndpointName[] destinations = _messageEndpointLookup.LookupEndpointsFor(typeof(T)).ToArray();
      PublishAt(publishAt, destinations, messages);
    }

    public void PublishAt<T>(DateTime publishAt, EndpointName destination, params T[] messages) where T : class, IMessage
    {
      PublishAt(publishAt, new[] { destination }, messages);
    }

    public void PublishAt<T>(DateTime publishAt, EndpointName[] destinations, params T[] messages) where T : class, IMessage
    {
      ISchedulePublishMessage message = _messageFactory.Create<ISchedulePublishMessage>();
      message.PublishAddresses = destinations;
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
      ScheduledPublish scheduled = new ScheduledPublish(message.PublishAt, message.MessagePayload, message.PublishAddresses, CurrentSagaContext.CurrentSagaId);
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
        using (CurrentSagaContext.Open(scheduled.SagaId))
        {
          foreach (EndpointName destination in scheduled.Addresses)
          {
            _bus.Send(destination, scheduled.MessagePayload);
          }
        }
      }
    }
  }
  [Serializable]
  public class ScheduledPublish
  {
    readonly DateTime _publishAt;
    readonly MessagePayload _messagePayload;
    readonly EndpointName[] _addresses;
    readonly Guid _sagaId;

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

    public Guid SagaId
    {
      get { return _sagaId; }
    }

    public ScheduledPublish(DateTime publishAt, MessagePayload messagePayload, EndpointName[] addresses, Guid sagaId)
    {
      _publishAt = publishAt;
      _messagePayload = messagePayload;
      _addresses = addresses;
      _sagaId = sagaId;
    }
  }
}
