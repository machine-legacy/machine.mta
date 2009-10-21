using System;
using System.Collections.Generic;
using System.Linq;

namespace Machine.Mta.Timing
{
  public class ScheduleFutureMessages : IScheduleFutureMessages
  {
    readonly IMessageFactory _messageFactory;
    readonly IMessageBus _bus;
    readonly IMessageRouting _routing;
    readonly MessagePayloadSerializer _messagePayloadSerializer;

    public ScheduleFutureMessages(IMessageFactory messageFactory, IMessageBus bus, IMessageRouting routing, MessagePayloadSerializer messagePayloadSerializer)
    {
      _messageFactory = messageFactory;
      _routing = routing;
      _messagePayloadSerializer = messagePayloadSerializer;
      _bus = bus;
    }

    public void PublishAt<T>(DateTime publishAt, params T[] messages) where T : IMessage
    {
      var destination = _routing.Owner(typeof(T));
      PublishAt(publishAt, destination, messages);
    }

    public void PublishAt<T>(DateTime publishAt, EndpointAddress destination, params T[] messages) where T : IMessage
    {
      PublishAt(publishAt, new[] { destination }, messages);
    }

    void PublishAt<T>(DateTime publishAt, IEnumerable<EndpointAddress> destinations, params T[] messages) where T : IMessage
    {
      _bus.Send(_messageFactory.Create<ISchedulePublishMessage>(m => {
        m.PublishAddresses = destinations.Select(d => d.ToString()).ToArray();
        m.PublishAt = publishAt;
        m.MessagePayload = _messagePayloadSerializer.Serialize(messages).ToByteArray();
      }));
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
  }

  public interface IScheduleFutureMessages
  {
    void PublishAt<T>(DateTime publishAt, params T[] messages) where T : IMessage;
    void PublishAt<T>(DateTime publishAt, EndpointAddress destination, params T[] messages) where T : IMessage;
  }
}
