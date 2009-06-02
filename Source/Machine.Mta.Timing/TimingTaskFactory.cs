using System;
using Machine.Container.Services;

namespace Machine.Mta.Timing
{
  public class TimingTaskFactory
  {
    readonly IMachineContainer _container;
    readonly IMessageBus _bus;
    readonly IMessageFactory _factory;

    public TimingTaskFactory(IMessageBus bus, IMessageFactory factory, IMachineContainer container)
    {
      _bus = bus;
      _container = container;
      _factory = factory;
    }

    public IOnceASecondTask Task<T>() where T : IOnceASecondTask
    {
      return _container.Resolve.Object<T>();
    }

    public IOnceASecondTask PublishMessage<T>(ITrigger trigger) where T : class, IMessage
    {
      return new PublishEmptyMessageTask<T>(_bus, _factory, trigger);
    }

    public IOnceASecondTask PublishMessage<T>(ITrigger trigger, Func<IMessageFactory, T> ctor) where T : class, IMessage
    {
      return new PublishFuncMessageTask<T>(_bus, _factory, trigger, ctor);
    }

    public IOnceASecondTask PublishMessage<T>(ITrigger trigger, object value) where T : class, IMessage
    {
      return PublishMessage(trigger, x => x.Create<T>(value));
    }
  }
}