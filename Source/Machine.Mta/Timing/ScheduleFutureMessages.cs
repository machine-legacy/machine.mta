using System;
using System.Linq;

namespace Machine.Mta.Timing
{
  public class ScheduleFutureMessages : IScheduleFutureMessages
  {
    readonly IMessageFactory _messageFactory;
    readonly IMessageBus _bus;
    readonly IMessageDestinations _messageDestinations;
    readonly MessagePayloadSerializer _messagePayloadSerializer;

    public ScheduleFutureMessages(IMessageFactory messageFactory, IMessageBus bus, IMessageDestinations messageDestinations, MessagePayloadSerializer messagePayloadSerializer)
    {
      _messageFactory = messageFactory;
      _messageDestinations = messageDestinations;
      _messagePayloadSerializer = messagePayloadSerializer;
      _bus = bus;
    }

    public void PublishAt<T>(DateTime publishAt, params T[] messages) where T : IMessage
    {
      EndpointAddress[] destinations = _messageDestinations.LookupEndpointsFor(typeof(T), true).ToArray();
      PublishAt(publishAt, destinations, messages);
    }

    public void PublishAt<T>(DateTime publishAt, EndpointAddress destination, params T[] messages) where T : IMessage
    {
      PublishAt(publishAt, new[] { destination }, messages);
    }

    public void PublishAt<T>(DateTime publishAt, EndpointAddress[] destinations, params T[] messages) where T : IMessage
    {
      ISchedulePublishMessage message = _messageFactory.Create<ISchedulePublishMessage>();
      message.PublishAddresses = destinations.Select(d => d.ToString()).ToArray();
      message.PublishAt = publishAt;
      message.MessagePayload = _messagePayloadSerializer.Serialize(messages).ToByteArray();
      _bus.Send(message);
    }
  }

  public class NullScheduleFutureMessages : IScheduleFutureMessages
  {
    public void PublishAt<T>(DateTime publishAt, params T[] messages) where T : IMessage
    {
    }

    public void PublishAt<T>(DateTime publishAt, EndpointAddress destination, params T[] messages) where T : IMessage
    {
    }

    public void PublishAt<T>(DateTime publishAt, EndpointAddress[] destinations, params T[] messages) where T : IMessage
    {
    }
  }

  public interface IScheduleFutureMessages
  {
    void PublishAt<T>(DateTime publishAt, params T[] messages) where T : IMessage;
    void PublishAt<T>(DateTime publishAt, EndpointAddress destination, params T[] messages) where T : IMessage;
    void PublishAt<T>(DateTime publishAt, EndpointAddress[] destinations, params T[] messages) where T : IMessage;
  }
}
