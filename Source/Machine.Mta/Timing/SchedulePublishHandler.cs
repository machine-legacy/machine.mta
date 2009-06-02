namespace Machine.Mta.Timing
{
  public class SchedulePublishHandler : IConsume<ISchedulePublishMessage>
  {
    readonly IScheduledPublishRepository _scheduledPublishRepository;

    public SchedulePublishHandler(IScheduledPublishRepository scheduledPublishRepository)
    {
      _scheduledPublishRepository = scheduledPublishRepository;
    }

    public void Consume(ISchedulePublishMessage message)
    {
      ScheduledPublish scheduled = new ScheduledPublish(message.PublishAt, message.MessagePayload, message.PublishAddresses, CurrentSagaContext.CurrentSagaIds(false));
      _scheduledPublishRepository.Add(scheduled);
    }
  }
}