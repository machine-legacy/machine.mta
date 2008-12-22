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

    public MsmqEndpoint(EndpointName name, MessageQueue queue)
    {
      _name = name;
      _queue = queue;
    }

    public void Send(TransportMessage transportMessage)
    {
      if (!_queue.CanWrite)
      {
        throw new InvalidOperationException("Queue is read-only: " + _name);
      }
      Message systemMessage = new Message();
      systemMessage.Label = transportMessage.ToString();
      systemMessage.Recoverable = true;
      systemMessage.TimeToBeReceived = TimeSpan.MaxValue;
      _formatter.Serialize(systemMessage.BodyStream, transportMessage);
      _queue.Send(systemMessage, MessageQueueTransactionType.Single);
    }

    public TransportMessage Receive(TimeSpan timeout)
    {
      if (!_queue.CanRead)
      {
        throw new InvalidOperationException("Queue is write-only: " + _name);
      }
      try
      {
        Message systemMessage = _queue.Receive(timeout, MessageQueueTransactionType.Single);
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
