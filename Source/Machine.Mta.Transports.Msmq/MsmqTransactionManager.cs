using System;
using System.Messaging;
using System.Transactions;

namespace Machine.Mta.Transports.Msmq
{
  public class MsmqTransactionManager
  {
    readonly ITransactionManager _transactionManager;

    public MsmqTransactionManager(ITransactionManager transactionManager)
    {
      _transactionManager = transactionManager;
    }

    public MessageQueueTransactionType SendTransactionType(MessageQueue queue)
    {
      return StandardTransactionType(queue);
    }

    public MessageQueueTransactionType ReceiveTransactionType(MessageQueue queue)
    {
      return StandardTransactionType(queue);
    }

    public virtual MessageQueueTransactionType StandardTransactionType(MessageQueue queue)
    {
      if (Transaction.Current == null)
      {
        return MessageQueueTransactionType.Single;
      }
      return MessageQueueTransactionType.Automatic;
    }

    public virtual TransactionScope CreateTransactionScope()
    {
      return _transactionManager.CreateTransactionScope();
    }
  }
}