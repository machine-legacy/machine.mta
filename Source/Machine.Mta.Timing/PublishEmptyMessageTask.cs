namespace Machine.Mta.Timing
{
  public class PublishEmptyMessageTask<T> : CreateMessageAndActOnItTask<T> where T : class, IMessage
  {
    public PublishEmptyMessageTask(IMessageBus bus, IMessageFactory messageFactory, ITrigger trigger)
      : base(bus, messageFactory, trigger, x => x.Create<T>(), (b, m) => b.Publish(m))
    {
    }
  }
}