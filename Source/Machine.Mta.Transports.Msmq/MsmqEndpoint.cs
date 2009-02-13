using System;
using System.Collections.Generic;
using System.Messaging;

using Machine.Mta.Endpoints;

namespace Machine.Mta.Transports.Msmq
{
  public class MsmqEndpoint : IEndpoint
  {
    readonly EndpointAddress _address;
    readonly MessageQueue _queue;
    readonly MsmqTransactionManager _transactionManager;

    public MsmqEndpoint(EndpointAddress address, MessageQueue queue, MsmqTransactionManager transactionManager)
    {
      _address = address;
      _transactionManager = transactionManager;
      _queue = queue;
    }

    public void Send(TransportMessage transportMessage)
    {
      if (!_queue.CanWrite)
      {
        throw new InvalidOperationException("Queue is read-only: " + _address);
      }
      Message systemMessage = new Message();
      systemMessage.Label = transportMessage.Label;
      systemMessage.Recoverable = true;
      systemMessage.TimeToBeReceived = TimeSpan.MaxValue;
      Serializers.Binary.Serialize(systemMessage.BodyStream, transportMessage);
      _queue.Send(systemMessage, _transactionManager.SendTransactionType(_queue));
      systemMessage.Dispose();
    }

    public TransportMessage Receive(TimeSpan timeout)
    {
      if (!_queue.CanRead)
      {
        System.Threading.Thread.Sleep(timeout);
        throw new InvalidOperationException("Queue is write-only: " + _address);
      }
      Message systemMessage = null;
      try
      {
        // This exception interferes with debugging apparently. This should mitigate that.
        if (System.Diagnostics.Debugger.IsAttached)
        {
          systemMessage = _queue.Receive(_transactionManager.ReceiveTransactionType(_queue));
        }
        else
        {
          systemMessage = _queue.Receive(timeout, _transactionManager.ReceiveTransactionType(_queue));
        }

        if (systemMessage == null)
        {
          return null;
        }
        return (TransportMessage)Serializers.Binary.Deserialize(systemMessage.BodyStream);
      }
      catch (MessageQueueException error)
      {
        if (error.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
        {
          return null;
        }
        throw;
      }
      finally
      {
        if (systemMessage != null)
        {
          systemMessage.Dispose();
        }
      }
    }
  }
}
