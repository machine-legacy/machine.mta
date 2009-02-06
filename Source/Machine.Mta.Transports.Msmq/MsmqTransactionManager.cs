using System;
using System.Messaging;
using System.Transactions;

namespace Machine.Mta.Transports.Msmq
{
  public class MsmqTransactionManager : ITransactionManager
  {
    public MessageQueueTransactionType SendTransactionType(MessageQueue queue)
    {
      return StandardTransactionType(queue);
    }

    public MessageQueueTransactionType ReceiveTransactionType(MessageQueue queue)
    {
      return StandardTransactionType(queue);
    }

    private static MessageQueueTransactionType StandardTransactionType(MessageQueue queue)
    {
      //if (queue.IsLocalQueue() && !queue.Transactional) return MessageQueueTransactionType.None;
      if (Transaction.Current == null)
      {
        return MessageQueueTransactionType.Single;
      }
      return MessageQueueTransactionType.Automatic;
    }

    public TransactionScope CreateTransactionScope()
    {
      return new TransactionScope();
    }
  } 
}