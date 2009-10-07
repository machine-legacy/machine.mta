using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Messaging;

using Machine.Mta.Endpoints;
using System.Linq;

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
      SerializedLabel label = new SerializedLabel(transportMessage);
      Message systemMessage = new Message();
      systemMessage.Label = label.ToLabel();
      systemMessage.Recoverable = true;
      systemMessage.TimeToBeReceived = TimeSpan.MaxValue;
      if (transportMessage.CorrelationId != null)
      {
        systemMessage.CorrelationId = transportMessage.CorrelationId;
      }
      systemMessage.ResponseQueue = new MessageQueue(transportMessage.ReturnAddress.ToNameAndHost().ToMsmqPath());
      systemMessage.BodyStream.Write(transportMessage.Body, 0, transportMessage.Body.Length);
      _queue.Send(systemMessage, _transactionManager.SendTransactionType(_queue));
      systemMessage.Dispose();
      transportMessage.Id = systemMessage.Id;
    }

    [DebuggerNonUserCode]
    public bool HasAnyPendingMessages(TimeSpan timeout)
    {
      CheckForWriteOnly(timeout);
      try
      {
        Message message = _queue.Peek(timeout);
        if (message != null)
        {
          message.Dispose();
        }
        return true;
      }
      catch (MessageQueueException error)
      {
        if (error.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
        {
          return false;
        }
        throw;
      }
    }

    public TransportMessage Receive(TimeSpan timeout)
    {
      CheckForWriteOnly(timeout);
      Message systemMessage = null;
      try
      {
        systemMessage = _queue.Receive(timeout, _transactionManager.ReceiveTransactionType(_queue));
        if (systemMessage == null)
        {
          return null;
        }

        SerializedLabel serializedLabel = SerializedLabel.Parse(systemMessage.Label);
        return TransportMessage.For(
          systemMessage.Id, 
          systemMessage.ResponseQueue.ToAddress(),
          systemMessage.CorrelationId,
          serializedLabel.SagaIds,
          systemMessage.BodyStream.ReadAsByteArray(),
          serializedLabel.Label
        );
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

    private void CheckForWriteOnly(TimeSpan timeout)
    {
      if (!_queue.CanRead)
      {
        System.Threading.Thread.Sleep(timeout);
        throw new InvalidOperationException("Queue is write-only: " + _address);
      }
    }
  }

  public class SerializedLabel
  {
    readonly string _label;
    readonly Guid[] _sagaIds;

    public string Label
    {
      get { return _label; }
    }

    public Guid[] SagaIds
    {
      get { return _sagaIds; }
    }

    public static SerializedLabel Empty = new SerializedLabel(String.Empty, new Guid[0]);

    public SerializedLabel(TransportMessage transportMessage)
    {
      _label = transportMessage.Label;
      _sagaIds = transportMessage.SagaIds;
    }

    public SerializedLabel(string label, Guid[] sagaIds)
    {
      _label = label;
      _sagaIds = sagaIds;
    }

    public static SerializedLabel Parse(string value)
    {
      try
      {
        if (String.IsNullOrEmpty(value)) return Empty;
        string[] parts = value.Split(':');
        if (parts.Length != 2) return Empty;
        string label = parts[0];
        string[] guidStrings = parts[1].Split(',');
        return new SerializedLabel(label, guidStrings.Where(x => !String.IsNullOrEmpty(x)).Select(x => new Guid(x)).ToArray());
      }
      catch (Exception error)
      {
        return Empty;
      }
    }

    public string ToLabel()
    {
      string label = _label + ":" + String.Join(",", _sagaIds.Select(x => x.ToString()).ToArray());
      if (label.Length > 250)
      {
        throw new InvalidOperationException("Serialized label would be too long: " + label.Length);
      }
      return label;
    }
  }

  public static class StreamHelper
  {
    public static byte[] ReadAsByteArray(this Stream stream)
    {
      using (MemoryStream destiny = new MemoryStream())
      {
        byte[] buffer = new byte[4096];
        while (true)
        {
          var bytesRead = stream.Read(buffer, 0, buffer.Length);
          if (bytesRead <= 0)
          {
            return destiny.ToArray();
          }
          destiny.Write(buffer, 0, bytesRead);
        }
      }
    }
  }
}
