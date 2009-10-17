using System.Linq;

namespace Machine.Mta.Timing
{
  public class PublishScheduledMessagesTask : IOnceASecondTask
  {
    readonly IScheduledPublishRepository _scheduledPublishRepository;
    readonly MessagePayloadSerializer _messagePayloadSerializer;
    readonly IMessageBus _bus;

    public PublishScheduledMessagesTask(IMessageBus bus, IScheduledPublishRepository scheduledPublishRepository, MessagePayloadSerializer messagePayloadSerializer)
    {
      _bus = bus;
      _messagePayloadSerializer = messagePayloadSerializer;
      _scheduledPublishRepository = scheduledPublishRepository;
    }

    public void OnceASecond()
    {
      foreach (var scheduled in _scheduledPublishRepository.FindAllExpired())
      {
        using (CurrentSagaContext.Open(scheduled.SagaIds))
        {
          var messages = _messagePayloadSerializer.Deserialize(new MessagePayload(scheduled.MessagePayload));
          foreach (var destination in scheduled.Addresses.Select(a => EndpointAddress.FromString(a)))
          {
            _bus.Send(destination, messages);
          }
        }
      }
    }
  }
}