using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Transactions;
using System.Linq;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using NServiceBus.Unicast.Transport.Msmq;
using NServiceBus.Serialization;
using NServiceBus.Utils;

namespace NServiceBus.Unicast.Transport.RabbitMQ
{
  public class RabbitMqTransport : ITransport
  {
    readonly Common.Logging.ILog _log = Common.Logging.LogManager.GetLogger(typeof(RabbitMqTransport));
    readonly ConnectionFactory _connectionFactory = new ConnectionFactory();
    readonly List<WorkerThread> _workers = new List<WorkerThread>();
    readonly ReaderWriterLockSlim _failuresPerMessageLocker = new ReaderWriterLockSlim();
    readonly IDictionary<string, Int32> _failuresPerMessage = new Dictionary<string, Int32>();
    AmqpAddress _listenAddress;
    AmqpAddress _poisonAddress;
    IMessageSerializer _messageSerializer;
    Int32 _numberOfWorkerThreads;
    Int32 _maximumNumberOfRetries;
    IsolationLevel _isolationLevel;
    TimeSpan _transactionTimeout = TimeSpan.FromMinutes(5);
    TimeSpan _receiveTimeout = TimeSpan.FromSeconds(2);
 
    class MessageReceiveProperties
    {
      public string MessageId { get; set; }
      public bool NeedToAbort { get; set; }
    }

    [ThreadStatic]
    static MessageReceiveProperties _messageContext;

    public RabbitMqTransport()
    {
      _connectionFactory.Parameters.UserName = ConnectionParameters.DefaultUser;
      _connectionFactory.Parameters.Password = ConnectionParameters.DefaultPass;
      _connectionFactory.Parameters.VirtualHost = ConnectionParameters.DefaultVHost;
    }

    public void Start()
    {
      for (var i = 0; i < _numberOfWorkerThreads; ++i)
      {
        AddWorkerThread().Start();
      }
    }

    public void ChangeNumberOfWorkerThreads(Int32 targetNumberOfWorkerThreads)
    {
      lock (_workers)
      {
        var numberOfThreads = _workers.Count;
        if (targetNumberOfWorkerThreads == numberOfThreads) return;
        if (targetNumberOfWorkerThreads < numberOfThreads)
        {
          for (var i = targetNumberOfWorkerThreads; i < numberOfThreads; i++)
          {
            _workers[i].Stop();
          }
        }
        else if (targetNumberOfWorkerThreads > numberOfThreads)
        {
          for (var i = numberOfThreads; i < targetNumberOfWorkerThreads; i++)
          {
            AddWorkerThread().Start();
          }
        }
      }
    }

    public void Send(TransportMessage transportMessage, string destination)
    {
      var address = AmqpAddress.FromString(destination);
      using (var stream = new MemoryStream())
      {
        this.MessageSerializer.Serialize(transportMessage.Body, stream);
        using (var connection = _connectionFactory.CreateConnection(address.Broker))
        {
          using (var channel = connection.CreateModel())
          {
            var messageId = Guid.NewGuid().ToString();
            var properties = channel.CreateBasicProperties();
            properties.MessageId = messageId;
            if (!String.IsNullOrEmpty(transportMessage.CorrelationId))
            {
              properties.CorrelationId = transportMessage.CorrelationId;
            }
            properties.Timestamp = DateTime.UtcNow.ToAmqpTimestamp();
            properties.ReplyTo = this.ListenAddress;
            properties.SetPersistent(transportMessage.Recoverable);
            channel.BasicPublish(address.Exchange, address.RoutingKey, properties, stream.ToArray());
            transportMessage.Id = properties.MessageId;
            _log.Info("Sent message " + transportMessage.Id + " to " + destination + " of " + transportMessage.Body[0].GetType().Name);
          }
        }
      }
    }

    public void ReceiveMessageLater(TransportMessage transportMessage)
    {
      if (!String.IsNullOrEmpty(this.ListenAddress))
      {
        Send(transportMessage, this.ListenAddress);
      }
    }

    public Int32 GetNumberOfPendingMessages()
    {
      return 0;
    }

    public void AbortHandlingCurrentMessage()
    {
      if (_messageContext != null)
      {
        _messageContext.NeedToAbort = true;
      }
    }

    public IList<Type> MessageTypesToBeReceived
    {
      set { this.MessageSerializer.Initialize(GetExtraTypes(value)); }
    }

    public Int32 NumberOfWorkerThreads
    {
      get
      {
        lock (_workers)
        {
          return _workers.Count;
        }
      }
      set { _numberOfWorkerThreads = value; }
    }

    public Int32 MaximumNumberOfRetries
    {
      get { return _maximumNumberOfRetries; }
      set { _maximumNumberOfRetries = value; }
    }

    public string Address
    {
      get
      {
        if (_listenAddress == null)
          return null;
        return _listenAddress.ToString();
      }
    }

    public void Dispose()
    {
      lock (_workers)
      {
        foreach (var worker in _workers)
        {
          worker.Stop();
        }
      }
    }

    public event EventHandler<TransportMessageReceivedEventArgs> TransportMessageReceived;
    public event EventHandler StartedMessageProcessing;
    public event EventHandler FinishedMessageProcessing;
    public event EventHandler<ThreadExceptionEventArgs> FailedMessageProcessing;

    public string ListenAddress
    {
      get { return _listenAddress.ToString(); }
      set { _listenAddress = String.IsNullOrEmpty(value) ? null : AmqpAddress.FromString(value); }
    }

    public string PoisonAddress
    {
      get { return _poisonAddress.ToString(); }
      set { _poisonAddress = String.IsNullOrEmpty(value) ? null : AmqpAddress.FromString(value); }
    }

    public IMessageSerializer MessageSerializer
    {
      get { return _messageSerializer; }
      set { _messageSerializer = value; }
    }

    public IsolationLevel IsolationLevel
    {
      get { return _isolationLevel; }
      set { _isolationLevel = value; }
    }

    public TimeSpan TransactionTimeout
    {
      get { return _transactionTimeout; }
      set { _transactionTimeout = value; }
    }

    public TimeSpan ReceiveTimeout
    {
      get { return _receiveTimeout; }
      set { _receiveTimeout = value; }
    }

    static Type[] GetExtraTypes(IEnumerable<Type> value)
    {
      var types = value.ToList();
      if (!types.Contains(typeof(List<object>)))
      {
        types.Add(typeof(List<object>));
      }
      return types.ToArray();
    }

    WorkerThread AddWorkerThread()
    {
      lock (_workers)
      {
        var newWorker = new WorkerThread(Process);
        _workers.Add(newWorker);
        newWorker.Stopped += ((sender, e) =>
        {
          var worker = sender as WorkerThread;
          lock (_workers)
          {
            _log.Info("Removing Worker");
            _workers.Remove(worker);
          }
        });
        return newWorker;
      }
    }

    void Process()
    {
      _messageContext = new MessageReceiveProperties();
      try
      {
        var wrapper = new TransactionWrapper();
        wrapper.RunInTransaction(() => Receive(_messageContext), _isolationLevel, _transactionTimeout);
        ClearFailuresForMessage(_messageContext.MessageId);
      }
      catch (AbortHandlingCurrentMessageException)
      {
      }
      catch (Exception error)
      {
        IncrementFailuresForMessage(_messageContext.MessageId);
        OnFailedMessageProcessing(error);
      }
      finally
      {
        _messageContext = null;
      }
    }

    void Receive(MessageReceiveProperties messageContext)
    {
      _log.Debug("Receiving from " + _listenAddress);
      using (var connection = _connectionFactory.CreateConnection(_listenAddress.Broker))
      {
        using (var channel = connection.CreateModel())
        {
          var consumer = new QueueingBasicConsumer(channel);
          channel.BasicConsume(_listenAddress.RoutingKey, false, null, consumer);
          {
            var delivery = consumer.Receive(_receiveTimeout);
            if (delivery != null)
            {
              DeliverMessage(channel, messageContext, delivery);
            }
          }
        }
      }
    }

    void DeliverMessage(IModel channel, MessageReceiveProperties messageContext, BasicDeliverEventArgs delivery)
    {
      messageContext.MessageId = delivery.BasicProperties.MessageId;
      if (HandledMaximumRetries(messageContext.MessageId))
      {
        MoveToPoison(delivery);
        channel.BasicAck(delivery.DeliveryTag, false);
        return;
      }

      var startedProcessingError = OnStartedMessageProcessing();
      if (startedProcessingError != null)
      {
        throw new MessageHandlingException("Exception occured while starting to process message.", startedProcessingError, null, null);
      }

      var m = new TransportMessage();
      try
      {
        using (var stream = new MemoryStream(delivery.Body))
        {
          m.Body = this.MessageSerializer.Deserialize(stream);
        }
      }
      catch (Exception deserializeError)
      {
        _log.Error("Could not extract message data.", deserializeError);
        MoveToPoison(delivery);
        OnFinishedMessageProcessing();
        return;
      }
      m.Id = delivery.BasicProperties.MessageId;
      m.CorrelationId = delivery.BasicProperties.CorrelationId;
      m.IdForCorrelation = delivery.BasicProperties.MessageId;
      m.ReturnAddress = delivery.BasicProperties.ReplyTo;
      m.TimeSent = delivery.BasicProperties.Timestamp.ToDateTime();
      m.Headers = new List<HeaderInfo>();
      m.Recoverable = delivery.BasicProperties.DeliveryMode == 2;
      var receivingError = OnTransportMessageReceived(m);
      var finishedProcessingError = OnFinishedMessageProcessing();
      if (messageContext.NeedToAbort)
      {
        throw new AbortHandlingCurrentMessageException();
      }
      if (receivingError != null || finishedProcessingError != null)
      {
        throw new MessageHandlingException("Exception occured while processing message.", null, receivingError, finishedProcessingError);
      }
      channel.BasicAck(delivery.DeliveryTag, false);
    }

    void IncrementFailuresForMessage(string id)
    {
      if (String.IsNullOrEmpty(id)) return;
      _failuresPerMessageLocker.EnterWriteLock();
      try
      {
        if (!_failuresPerMessage.ContainsKey(id))
        {
          _failuresPerMessage[id] = 1;
        }
        else
        {
          _failuresPerMessage[id] += 1;
        }
      }
      finally
      {
        _failuresPerMessageLocker.ExitWriteLock();
      }
    }

    void ClearFailuresForMessage(string id)
    {
      if (String.IsNullOrEmpty(id)) return;
      _failuresPerMessageLocker.EnterReadLock();
      if (_failuresPerMessage.ContainsKey(id))
      {
        _failuresPerMessageLocker.ExitReadLock();
        _failuresPerMessageLocker.EnterWriteLock();
        _failuresPerMessage.Remove(id);
        _failuresPerMessageLocker.ExitWriteLock();
      }
      else
      {
        _failuresPerMessageLocker.ExitReadLock();
      }
    }
    
    bool HandledMaximumRetries(string id)
    {
      if (String.IsNullOrEmpty(id)) return false;
      _failuresPerMessageLocker.EnterReadLock();
      if (_failuresPerMessage.ContainsKey(id) && (_failuresPerMessage[id] == _maximumNumberOfRetries))
      {
        _failuresPerMessageLocker.ExitReadLock();
        _failuresPerMessageLocker.EnterWriteLock();
        _failuresPerMessage.Remove(id);
        _failuresPerMessageLocker.ExitWriteLock();
        return true;
      }
      _failuresPerMessageLocker.ExitReadLock();
      return false;
    }

    void MoveToPoison(BasicDeliverEventArgs delivery)
    {
      if (_poisonAddress == null)
      {
        _log.Info("Discarding " + delivery.BasicProperties.MessageId);
        return;
      }
      using (var connection = _connectionFactory.CreateConnection(_poisonAddress.Broker))
      {
        using (var channel = connection.CreateModel())
        {
          _log.Info("Moving " + delivery.BasicProperties.MessageId + " to " + _poisonAddress);
          channel.BasicPublish(_poisonAddress.Exchange, _poisonAddress.RoutingKey, delivery.BasicProperties, delivery.Body);
        }
      }
    }

    Exception OnFailedMessageProcessing(Exception error)
    {
      try
      {
        if (this.FailedMessageProcessing != null)
        {
          this.FailedMessageProcessing(this, new ThreadExceptionEventArgs(error));
        }
      }
      catch (Exception processingError)
      {
        _log.Error("Failed raising 'failed message processing' event.", processingError);
        return processingError;
      }
      return null;
    }

    Exception OnStartedMessageProcessing()
    {
      try
      {
        if (this.StartedMessageProcessing != null)
        {
          this.StartedMessageProcessing(this, null);
        }
      }
      catch (Exception processingError)
      {
        _log.Error("Failed raising 'started message processing' event.", processingError);
        return processingError;
      }
      return null;
    }
    
    Exception OnFinishedMessageProcessing()
    {
      try
      {
        if (this.FinishedMessageProcessing != null)
        {
          this.FinishedMessageProcessing(this, null);
        }
      }
      catch (Exception processingError)
      {
        _log.Error("Failed raising 'finished message processing' event.", processingError);
        return processingError;
      }
      return null;
    }
    
    Exception OnTransportMessageReceived(TransportMessage msg)
    {
      try
      {
        if (this.TransportMessageReceived != null)
        {
          this.TransportMessageReceived(this, new TransportMessageReceivedEventArgs(msg));
        }
      }
      catch (Exception processingError)
      {
        _log.Error("Failed raising 'transport message received' event.", processingError);
        return processingError;
      }
      return null;
    }
  }

  public static class ConsumerHelpers
  {
    static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);

    public static BasicDeliverEventArgs Receive(this QueueingBasicConsumer consumer, TimeSpan to)
    {
      object delivery;
      if (!consumer.Queue.Dequeue((Int32)to.TotalMilliseconds, out delivery))
      {
        return null;
      }
      return delivery as BasicDeliverEventArgs;
    }

    public static DateTime ToDateTime(this AmqpTimestamp timestamp)
    {
      return UnixEpoch.AddSeconds(timestamp.UnixTime);
    }

    public static AmqpTimestamp ToAmqpTimestamp(this DateTime dateTime)
    {
      return new AmqpTimestamp((long)(dateTime - UnixEpoch).TotalSeconds);
    }
  }
}
