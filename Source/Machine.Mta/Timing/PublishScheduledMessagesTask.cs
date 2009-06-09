using System.Linq;

namespace Machine.Mta.Timing
{
  public class PublishScheduledMessagesTask : IOnceASecondTask
  {
    readonly IScheduledPublishRepository _scheduledPublishRepository;
    readonly TransportMessageBodySerializer _transportMessageBodySerializer;
    readonly IMessageBus _bus;

    public PublishScheduledMessagesTask(IMessageBus bus, IScheduledPublishRepository scheduledPublishRepository, TransportMessageBodySerializer transportMessageBodySerializer)
    {
      _bus = bus;
      _transportMessageBodySerializer = transportMessageBodySerializer;
      _scheduledPublishRepository = scheduledPublishRepository;
    }

    public void OnceASecond()
    {
      foreach (ScheduledPublish scheduled in _scheduledPublishRepository.FindAllExpired())
      {
        using (CurrentSagaContext.Open(scheduled.SagaIds))
        {
          var messages = _transportMessageBodySerializer.Deserialize(scheduled.MessagePayload);
          foreach (EndpointAddress destination in scheduled.Addresses.Select(a => EndpointAddress.FromString(a)))
          {
            _bus.Send(destination, messages);
          }
        }
      }
    }
  }
}