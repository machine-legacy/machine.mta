using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Transactions;
using System.Linq;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

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
    string _listenAddress;
    string _poisonAddress;
    IMessageSerializer _messageSerializer;
    Int32 _numberOfWorkerThreads;
    Int32 _maximumNumberOfRetries;
    IsolationLevel _isolationLevel;
 
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
      using (var stream = new MemoryStream())
      {
        this.MessageSerializer.Serialize(transportMessage.Body, stream);
        using (var connection = _connectionFactory.CreateConnection("192.168.0.173"))
        {
          using (var channel = connection.CreateModel())
          {
            var properties = channel.CreateBasicProperties();
            properties.MessageId = Guid.NewGuid().ToString();
            // properties.CorrelationId = null;
            properties.Timestamp = DateTime.UtcNow.ToAmqpTimestamp();
            properties.ReplyTo = this.ListenAddress;
            // properties.SetPersistent(true);
            channel.BasicPublish("www", "", properties, stream.ToArray());
            transportMessage.Id = properties.MessageId;
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
      get { return _listenAddress; }
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
    public event EventHandler FailedMessageProcessing;

    public string ListenAddress
    {
      get { return _listenAddress; }
      set { _listenAddress = value; }
    }

    public string PoisonAddress
    {
      get { return _poisonAddress; }
      set { _poisonAddress = value; }
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
          _log.Info("Stopped Worker: " + sender);
          var worker = sender as WorkerThread;
          lock (_workers)
          {
            _workers.Remove(worker);
          }
        });
        return newWorker;
      }
    }

    void Process()
    {
      var id = String.Empty;
      try
      {
        new TransactionWrapper().RunInTransaction(Receive, _isolationLevel, TimeSpan.FromMinutes(5));
        // ClearFailuresForMessage(id);
      }
      catch (Exception error)
      {
        _log.Error(error);
        // IncrementFailuresForMessage(id);
        // OnFailedMessageProcessing(error);
      }
    }

    void Receive()
    {
      using (var connection = _connectionFactory.CreateConnection("192.168.0.173"))
      {
        using (var channel = connection.CreateModel())
        {
          var consumer = new QueueingBasicConsumer(channel);
          channel.BasicConsume("test1", true, null, consumer);
          var delivery = consumer.Receive(TimeSpan.FromSeconds(1));
          if (delivery != null)
          {
            if (this.StartedMessageProcessing != null)
            {
              this.StartedMessageProcessing(this, null);
            }
            using (var stream = new MemoryStream(delivery.Body))
            {
              var m = new TransportMessage();
              try
              {
                m.Body =  this.MessageSerializer.Deserialize(stream);
              }
              catch (Exception deserializeError)
              {
                _log.Error(deserializeError);
                // this.MoveToErrorQueue(m);
                // this.OnFinishedMessageProcessing();
                return;
              }
              m.Id = delivery.BasicProperties.MessageId;
              m.CorrelationId = delivery.BasicProperties.CorrelationId;
              m.IdForCorrelation = delivery.BasicProperties.CorrelationId;
              m.ReturnAddress = delivery.BasicProperties.ReplyTo;
              m.TimeSent = delivery.BasicProperties.Timestamp.ToDateTime();
              m.Headers = new List<HeaderInfo>();
              // m.TimeToBeReceived = delivery.BasicProperties.Expiration;
              m.Recoverable = false;
              OnTransportMessageReceived(m);
            }
          }
          channel.Close(200, "Done");
        }
      }
    }

    void IncrementFailuresForMessage(string id)
    {
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
        _log.Error(processingError);
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
      catch (Exception error)
      {
        _log.Error(error);
        return error;
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
