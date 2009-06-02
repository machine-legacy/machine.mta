namespace Machine.Mta.Timing
{
  public class PublishEmptyMessageTask<T> : PublishFuncMessageTask<T> where T : class, IMessage
  {
    public PublishEmptyMessageTask(IMessageBus bus, IMessageFactory messageFactory, ITrigger trigger)
      : base(bus, messageFactory, trigger, x => x.Create<T>())
    {
    }
  }
}