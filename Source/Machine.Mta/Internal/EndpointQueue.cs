using System;
using System.Transactions;

using Machine.Core.Services;
using Machine.Utility.ThreadPool;

namespace Machine.Mta.Internal
{
  public class EndpointQueue : IQueue
  {
    readonly ITransactionManager _transactionManager;
    readonly IEndpoint _listeningOn;
    readonly Action<object> _dispatcher;

    public EndpointQueue(ITransactionManager transactionManager, IEndpoint listeningOn, Action<object> dispatcher)
    {
      _transactionManager = transactionManager;
      _listeningOn = listeningOn;
      _dispatcher = dispatcher;
    }

    public void Enqueue(IRunnable runnable)
    {
      throw new NotSupportedException();
    }

    public IScope CreateScope()
    {
      return new EndpointScope(_listeningOn, _dispatcher, _transactionManager.CreateTransactionScope());
    }

    public void Drainstop()
    {
    }
  }
  public class EndpointScope : IScope
  {
    static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(EndpointScope));
    readonly IEndpoint _listeningOn;
    readonly Action<object> _dispatcher;
    readonly TransactionScope _scope;

    public EndpointScope(IEndpoint listeningOn, Action<object> dispatcher, TransactionScope scope)
    {
      _listeningOn = listeningOn;
      _dispatcher = dispatcher;
      _scope = scope;
    }

    public IRunnable Dequeue()
    {
      try
      {
        object value = _listeningOn.Receive(TimeSpan.FromSeconds(3));
        if (value == null)
        {
          return null;
        }
        return new DispatcherRunnable(_dispatcher, value);
      }
      catch (Exception error)
      {
        _log.Error(error);
        return null;
      }
    }

    public void Complete()
    {
      _scope.Complete();
    }

    public void Dispose()
    {
      _scope.Dispose();
    }
  }
  public class DispatcherRunnable : IRunnable
  {
    readonly Action<object> _dispatcher;
    readonly object _value;

    public DispatcherRunnable(Action<object> dispatcher, object value)
    {
      _dispatcher = dispatcher;
      _value = value;
    }

    public void Run()
    {
      _dispatcher(_value);
    }
  }
}