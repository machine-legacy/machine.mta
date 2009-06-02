namespace Machine.Mta.Timing
{
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
        using (CurrentSagaContext.Open(scheduled.SagaIds))
        {
          foreach (EndpointAddress destination in scheduled.Addresses)
          {
            _bus.Send(destination, scheduled.MessagePayload);
          }
        }
      }
    }
  }
}