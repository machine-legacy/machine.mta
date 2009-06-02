using System;
using System.Linq;

namespace Machine.Mta.Timing
{
  public class ScheduleFutureMessages : IScheduleFutureMessages
  {
    readonly IMessageFactory _messageFactory;
    readonly IMessageBus _bus;
    readonly IMessageDestinations _messageDestinations;
    readonly TransportMessageBodySerializer _transportMessageBodySerializer;

    public ScheduleFutureMessages(IMessageFactory messageFactory, IMessageBus bus, IMessageDestinations messageDestinations, TransportMessageBodySerializer transportMessageBodySerializer)
    {
      _messageFactory = messageFactory;
      _messageDestinations = messageDestinations;
      _transportMessageBodySerializer = transportMessageBodySerializer;
      _bus = bus;
    }

    public void PublishAt<T>(DateTime publishAt, params T[] messages) where T : class, IMessage
    {
      EndpointAddress[] destinations = _messageDestinations.LookupEndpointsFor(typeof(T), true).ToArray();
      PublishAt(publishAt, destinations, messages);
    }

    public void PublishAt<T>(DateTime publishAt, EndpointAddress destination, params T[] messages) where T : class, IMessage
    {
      PublishAt(publishAt, new[] { destination }, messages);
    }

    public void PublishAt<T>(DateTime publishAt, EndpointAddress[] destinations, params T[] messages) where T : class, IMessage
    {
      ISchedulePublishMessage message = _messageFactory.Create<ISchedulePublishMessage>();
      message.PublishAddresses = destinations;
      message.PublishAt = publishAt;
      message.MessagePayload = _transportMessageBodySerializer.Serialize(messages);
      _bus.Send(message);
    }
  }

  public interface IScheduleFutureMessages
  {
    void PublishAt<T>(DateTime publishAt, params T[] messages) where T : class, IMessage;
    void PublishAt<T>(DateTime publishAt, EndpointAddress destination, params T[] messages) where T : class, IMessage;
    void PublishAt<T>(DateTime publishAt, EndpointAddress[] destinations, params T[] messages) where T : class, IMessage;
  }
}
