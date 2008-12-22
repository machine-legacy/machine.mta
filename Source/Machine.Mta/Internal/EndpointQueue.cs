using System;

using Machine.Core.Services;
using Machine.Utility.ThreadPool;

using MassTransit;

namespace Machine.Mta.Internal
{
  public class EndpointQueue : IQueue
  {
    static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(EndpointQueue));
    readonly IEndpoint _listeningOn;
    readonly Action<object> _dispatcher;

    public EndpointQueue(IEndpoint listeningOn, Action<object> dispatcher)
    {
      _listeningOn = listeningOn;
      _dispatcher = dispatcher;
    }

    public void Enqueue(IRunnable runnable)
    {
      throw new NotSupportedException();
    }

    public IRunnable Dequeue()
    {
      try
      {
        object received = _listeningOn.Receive(TimeSpan.FromSeconds(3), x => x is TransportMessage);
        if (received == null)
        {
          return null;
        }
        return new DispatcherRunnable(received, _dispatcher);
      }
      catch (Exception error)
      {
        _log.Error(error);
        return null;
      }
    }

    public void Drainstop()
    {
    }

    class DispatcherRunnable : IRunnable
    {
      readonly object _value;
      readonly Action<object> _dispatcher;

      public DispatcherRunnable(object value, Action<object> dispatcher)
      {
        _value = value;
        _dispatcher = dispatcher;
      }

      public void Run()
      {
        _dispatcher(_value);
      }
    }
  }
}