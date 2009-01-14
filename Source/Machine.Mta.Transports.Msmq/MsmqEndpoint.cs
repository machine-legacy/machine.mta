using System;
using System.Collections.Generic;
using System.Messaging;
using System.Runtime.Serialization.Formatters.Binary;

using Machine.Mta.Internal;

namespace Machine.Mta.Transports.Msmq
{
  public class MsmqEndpoint : IEndpoint
  {
    readonly BinaryFormatter _formatter = new BinaryFormatter();
    readonly EndpointName _name;
    readonly MessageQueue _queue;
    readonly MsmqTransactionManager _transactionManager;

    public MsmqEndpoint(EndpointName name, MessageQueue queue, MsmqTransactionManager transactionManager)
    {
      _name = name;
      _transactionManager = transactionManager;
      _queue = queue;
    }

    public void Send(TransportMessage transportMessage)
    {
      if (!_queue.CanWrite)
      {
        throw new InvalidOperationException("Queue is read-only: " + _name);
      }
      Message systemMessage = new Message();
      systemMessage.Label = transportMessage.Label;
      systemMessage.Recoverable = true;
      systemMessage.TimeToBeReceived = TimeSpan.MaxValue;
      _formatter.Serialize(systemMessage.BodyStream, transportMessage);
      _queue.Send(systemMessage, _transactionManager.SendTransactionType(_queue));
    }

    public TransportMessage Receive(TimeSpan timeout)
    {
      if (!_queue.CanRead)
      {
        System.Threading.Thread.Sleep(timeout);
        throw new InvalidOperationException("Queue is write-only: " + _name);
      }
      try
      {
        Message systemMessage = _queue.Receive(timeout, _transactionManager.ReceiveTransactionType(_queue));
        if (systemMessage == null)
        {
          return null;
        }
        return (TransportMessage)_formatter.Deserialize(systemMessage.BodyStream);
      }
      catch (MessageQueueException error)
      {
        if (error.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
        {
          return null;
        }
        throw;
      }
    }
  }
}
