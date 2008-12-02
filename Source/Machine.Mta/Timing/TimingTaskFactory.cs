using System;

using Machine.Mta.InterfacesAsMessages;

namespace Machine.Mta.Timing
{
  public class TimingTaskFactory
  {
    readonly IMessageBus _bus;
    readonly IMessageFactory _factory;

    public TimingTaskFactory(IMessageBus bus, IMessageFactory factory)
    {
      _bus = bus;
      _factory = factory;
    }

    public IOnceASecondTask PublishMessage<T>(ITrigger trigger) where T : class, IMessage
    {
      return new PublishMessageTask<T>(_bus, _factory, trigger);
    }
  }
}