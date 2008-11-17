using System;
using System.Threading;

using MassTransit.ServiceBus.Services.Timeout;
using MassTransit.ServiceBus.Util;

namespace Machine.Mta.Timeouts
{
  public interface ITimeoutService : IDisposable
  {
    void Start();
    void Stop();
  }
  public class TimeoutService : ITimeoutService
  {
    static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(TimeoutService));
    static readonly TimeSpan _interval = TimeSpan.FromSeconds(1.0);
    readonly ManualResetEvent _stopped = new ManualResetEvent(false);
    readonly AutoResetEvent _trigger = new AutoResetEvent(true);
    readonly IMessageBus _bus;
    readonly ITimeoutRepository _repository;
    readonly IMessageFactory _messageFactory;
    Thread _thread;

    public TimeoutService(IMessageBus bus, ITimeoutRepository repository, IMessageFactory messageFactory)
    {
      _bus = bus;
      _messageFactory = messageFactory;
      _repository = repository;
    }

    public void Start()
    {
      _repository.TimeoutAdded += TriggerPublisher;
      _thread = new Thread(PublishPendingTimeoutMessages);
      _thread.IsBackground = true;
      _thread.Start();
    }

    public void Stop()
    {
    }

    public void Dispose()
    {
    }

    private void TriggerPublisher(Guid obj)
    {
      _trigger.Set();
    }

    private void PublishPendingTimeoutMessages()
    {
      try
      {
        WaitHandle[] handles = new WaitHandle[] { _trigger, _stopped };
        while (WaitHandle.WaitAny(handles, _interval, true) != 1)
        {
          DateTime lessThan = DateTime.UtcNow;
          foreach (Tuple<Guid, DateTime> tuple in _repository.List(lessThan))
          {
            ITimeoutExpiredMessage message = _messageFactory.Create<ITimeoutExpiredMessage>();
            message.CorrelationId = tuple.Key;
            _bus.Publish(message);
            _repository.Remove(tuple.Key);
          }
        }
      }
      catch (Exception error)
      {
        _log.Error(error);
      }
    }
  }
}