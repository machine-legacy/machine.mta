using System;
using System.Transactions;

using Machine.Mta.Endpoints;
using Machine.Utility.ThreadPool;
using Machine.Utility.ThreadPool.Workers;

namespace Machine.Mta.Dispatching
{
  public class WorkerFactory : IWorkerFactory
  {
    readonly IMessageBus _bus;
    readonly IEndpoint _queue;
    readonly IEndpoint _poison;
    readonly ITransactionManager _transactionManager;
    readonly IMessageFailureManager _messageFailureManager;
    readonly Action<TransportMessage> _dispatcher;

    public WorkerFactory(IMessageBus bus, ITransactionManager transactionManager, IMessageFailureManager messageFailureManager, IEndpoint queue, IEndpoint poison, Action<TransportMessage> dispatcher)
    {
      _bus = bus;
      _queue = queue;
      _poison= poison;
      _transactionManager = transactionManager;
      _messageFailureManager = messageFailureManager;
      _dispatcher = dispatcher;
    }

    public Worker CreateWorker(BusyWatcher busyWatcher)
    {
      return new BusWorker(busyWatcher, _bus, _queue, _poison, _transactionManager, _messageFailureManager, _dispatcher);
    }
  }

  public class BusWorker : AbstractWorker
  {
    readonly static TimeSpan TimeForEachRead = TimeSpan.FromSeconds(3.0);
    readonly IMessageBus _bus;
    readonly IEndpoint _queue;
    readonly IEndpoint _poison;
    readonly ITransactionManager _transactionManager;
    readonly IMessageFailureManager _messageFailureManager;
    readonly Action<TransportMessage> _dispatcher;
    
    public BusWorker(BusyWatcher busyWatcher, IMessageBus bus, IEndpoint queue, IEndpoint poison, ITransactionManager transactionManager, IMessageFailureManager messageFailureManager, Action<TransportMessage> dispatcher)
      : base(busyWatcher)
    {
      _transactionManager = transactionManager;
      _bus = bus;
      _messageFailureManager = messageFailureManager;
      _queue = queue;
      _poison = poison;
      _dispatcher = dispatcher;
    }

    public override void WhileAlive()
    {
      if (_queue.HasAnyPendingMessages(TimeForEachRead))
      {
        TransportMessage transportMessage = null;
        try
        {
          using (TransactionScope scope = _transactionManager.CreateTransactionScope())
          {
            transportMessage = _queue.Receive(TimeForEachRead);
            if (transportMessage != null)
            {
              using (CurrentMessageBus.Open(_bus))
              {
                using (CurrentMessageContext.Open(transportMessage))
                {
                  if (_messageFailureManager.IsPoison(transportMessage))
                  {
                    Logging.Poison(transportMessage);
                    _poison.Send(transportMessage);
                  }
                  else
                  {
                    _dispatcher(transportMessage);
                  }
                }
              }
            }
            scope.Complete();
          }
        }
        catch (Exception error)
        {
          base.Error(error);
          _messageFailureManager.RecordFailure(_bus.Address, transportMessage, error);
        }
      }
    }
  }
}