using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus;

namespace Machine.Mta.Timing
{
  public class ScheduleFutureMessages : IScheduleFutureMessages
  {
    readonly IMessageFactory _messageFactory;
    readonly IMessageBus _bus;
    readonly MessagePayloadSerializer _messagePayloadSerializer;
    readonly IMessageRouting _routing;

    public ScheduleFutureMessages(IMessageFactory messageFactory, IMessageBus bus, MessagePayloadSerializer messagePayloadSerializer, IMessageRouting routing)
    {
      _messageFactory = messageFactory;
      _routing = routing;
      _messagePayloadSerializer = messagePayloadSerializer;
      _bus = bus;
    }

    public void SendAt<T>(DateTime publishAt, EndpointAddress destination, params T[] messages) where T : IMessage
    {
      _bus.Send(_messageFactory.Create<ISchedulePublishMessage>(m => {
        m.PublishAddresses = new[] { destination }.Select(d => d.ToString()).ToArray();
        m.PublishAt = publishAt;
        m.MessagePayload = _messagePayloadSerializer.Serialize(messages).ToByteArray();
      }));
    }

    public void PublishAt<T>(DateTime publishAt, params T[] messages) where T : IMessage
    {
      var destinations = _routing.Subscribers(typeof(T));
      _bus.Send(_messageFactory.Create<ISchedulePublishMessage>(m => {
        m.PublishAddresses = destinations.Select(d => d.ToString()).ToArray();
        m.PublishAt = publishAt;
        m.MessagePayload = _messagePayloadSerializer.Serialize(messages).ToByteArray();
      }));
    }
  }

  public class NullScheduleFutureMessages : IScheduleFutureMessages
  {
    public void SendAt<T>(DateTime publishAt, EndpointAddress destination, params T[] messages) where T : IMessage
    {
    }

    public void PublishAt<T>(DateTime publishAt, params T[] messages) where T : IMessage
    {
    }
  }

  public interface IScheduleFutureMessages
  {
    void SendAt<T>(DateTime publishAt, EndpointAddress destination, params T[] messages) where T : IMessage;
    void PublishAt<T>(DateTime publishAt, params T[] messages) where T : IMessage;
  }
}
