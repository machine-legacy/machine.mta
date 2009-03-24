using System;
using System.Transactions;

using Machine.Mta.Endpoints;
using Machine.Utility.ThreadPool;
using Machine.Utility.ThreadPool.Workers;

namespace Machine.Mta.Dispatching
{
  public class WorkerFactory : IWorkerFactory
  {
    readonly ITransactionManager _transactionManager;
    readonly IEndpoint _endpoint;
    readonly IMessageFailureManager _messageFailureManager;
    readonly Action<TransportMessage> _dispatcher;

    public WorkerFactory(ITransactionManager transactionManager, IMessageFailureManager messageFailureManager, IEndpoint endpoint, Action<TransportMessage> dispatcher)
    {
      _transactionManager = transactionManager;
      _messageFailureManager = messageFailureManager;
      _dispatcher = dispatcher;
      _endpoint = endpoint;
    }

    public Worker CreateWorker(BusyWatcher busyWatcher)
    {
      return new BusWorker(busyWatcher, _transactionManager, _messageFailureManager, _endpoint, _dispatcher);
    }
  }

  public class BusWorker : AbstractWorker
  {
    readonly static TimeSpan TimeForEachRead = TimeSpan.FromSeconds(3.0);
    readonly ITransactionManager _transactionManager;
    readonly IMessageFailureManager _messageFailureManager;
    readonly IEndpoint _endpoint;
    readonly Action<TransportMessage> _dispatcher;
    
    public BusWorker(BusyWatcher busyWatcher, ITransactionManager transactionManager, IMessageFailureManager messageFailureManager, IEndpoint endpoint, Action<TransportMessage> dispatcher)
      : base(busyWatcher)
    {
      _transactionManager = transactionManager;
      _messageFailureManager = messageFailureManager;
      _dispatcher = dispatcher;
      _endpoint = endpoint;
    }

    public override void WhileAlive()
    {
      if (_endpoint.HasAnyPendingMessages(TimeForEachRead))
      {
        TransportMessage transportMessage = null;
        try
        {
          using (TransactionScope scope = _transactionManager.CreateTransactionScope())
          {
            transportMessage = _endpoint.Receive(TimeForEachRead);
            _dispatcher(transportMessage);
            scope.Complete();
          }
        }
        catch (Exception error)
        {
          if (transportMessage != null)
          {
            _messageFailureManager.RecordFailure(transportMessage, error);
          }
          base.Error(error);
        }
      }
    }
  }
}