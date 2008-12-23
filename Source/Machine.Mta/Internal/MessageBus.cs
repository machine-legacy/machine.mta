using System;
using System.Collections.Generic;

using Machine.Utility.ThreadPool;
using Machine.Utility.ThreadPool.QueueStrategies;

namespace Machine.Mta.Internal
{
  public class MessageFailureManager
  {
    readonly Dictionary<Guid, List<Exception>> _errors = new Dictionary<Guid, List<Exception>>();
    readonly object _lock = new object();

    public void RecordFailure(Guid id, Exception error)
    {
      lock (_lock)
      {
        if (!_errors.ContainsKey(id))
        {
          _errors[id] = new List<Exception>();
        }
        _errors[id].Add(error);
      }
    }

    public bool SendToPoisonQueue(Guid id)
    {
      lock (_lock)
      {
        bool hasErrors = _errors.ContainsKey(id);
        if (hasErrors)
        {
          _errors.Remove(id);
        }
        return hasErrors;
      }
    }
  }
  public class ReturnAddressProvider
  {
    public virtual EndpointName GetReturnAddress(EndpointName listeningOn)
    {
      if (listeningOn.IsLocal)
      {
        return EndpointName.ForRemoteQueue(Environment.MachineName, listeningOn.Name);
      }
      return listeningOn;
    }
  }
  public class MessageBus : IMessageBus
  {
    private static readonly log4net.ILog _receivingLog = log4net.LogManager.GetLogger(typeof(MessageBus).FullName + ".Receiving");
    private static readonly log4net.ILog _poisonLog = log4net.LogManager.GetLogger(typeof(MessageBus).FullName + ".Poison");
    private static readonly log4net.ILog _sendingLog = log4net.LogManager.GetLogger(typeof(MessageBus).FullName + ".Sending");
    private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(MessageBus));
    private readonly IMessageEndpointLookup _messageEndpointLookup;
    private readonly IEndpointResolver _endpointResolver;
    private readonly IMessageDispatcher _dispatcher;
    private readonly IEndpoint _listeningOn;
    private readonly IEndpoint _poison;
    private readonly EndpointName _listeningOnEndpointName;
    private readonly EndpointName _poisonEndpointName;
    private readonly ThreadPool _threads;
    private readonly TransportMessageBodySerializer _transportMessageBodySerializer;
    private readonly AsyncCallbackMap _asyncCallbackMap;
    private readonly MessageFailureManager _messageFailureManager;
    private readonly ReturnAddressProvider _returnAddressProvider;
    private readonly ITransactionManager _transactionManager;

    public MessageBus(IEndpointResolver endpointResolver, IMessageEndpointLookup messageEndpointLookup, TransportMessageBodySerializer transportMessageBodySerializer, IMessageDispatcher dispatcher, EndpointName listeningOnEndpointName, EndpointName poisonEndpointName, ITransactionManager transactionManager, ThreadPoolConfiguration threadPoolConfiguration)
    {
      _endpointResolver = endpointResolver;
      _transactionManager = transactionManager;
      _dispatcher = dispatcher;
      _transportMessageBodySerializer = transportMessageBodySerializer;
      _messageEndpointLookup = messageEndpointLookup;
      _listeningOn = _endpointResolver.Resolve(listeningOnEndpointName);
      _poison = _endpointResolver.Resolve(poisonEndpointName);
      _listeningOnEndpointName = listeningOnEndpointName;
      _poisonEndpointName = poisonEndpointName;
      _messageEndpointLookup = messageEndpointLookup;
      _threads = new ThreadPool(threadPoolConfiguration, new SingleQueueStrategy(new EndpointQueue(_transactionManager, _listeningOn, EndpointDispatcher)));
      _asyncCallbackMap = new AsyncCallbackMap();
      _messageFailureManager = new MessageFailureManager();
      _returnAddressProvider = new ReturnAddressProvider();
    }

    public MessageBus(IEndpointResolver endpointResolver, IMessageEndpointLookup messageEndpointLookup, TransportMessageBodySerializer transportMessageBodySerializer, IMessageDispatcher dispatcher, EndpointName listeningOnEndpointName, EndpointName poisonEndpointName, ITransactionManager transactionManager)
      : this(endpointResolver, messageEndpointLookup, transportMessageBodySerializer, dispatcher, listeningOnEndpointName, poisonEndpointName, transactionManager, new ThreadPoolConfiguration(1, 1))
    {
    }

    public EndpointName PoisonAddress
    {
      get { return _poisonEndpointName; }
    }

    public EndpointName Address
    {
      get { return _listeningOnEndpointName; }
    }

    public void ChangeThreadPoolConfiguration(ThreadPoolConfiguration configuration)
    {
      _threads.ChangeConfiguration(configuration);
    }

    public void Start()
    {
      _log.Info("Starting");
      _threads.Start();
    }

    public void Send<T>(params T[] messages) where T : class, IMessage
    {
      foreach (T message in messages) _sendingLog.Info("Sending " + message);
      SendTransportMessage<T>(CreateTransportMessage(Guid.Empty, messages));
    }

    public void Send<T>(EndpointName destination, params T[] messages) where T : class, IMessage
    {
      foreach (T message in messages) _sendingLog.Info("Sending " + message + " to " + destination);
      SendTransportMessage(new[] { destination }, CreateTransportMessage(Guid.Empty, messages));
    }

    public void Send(EndpointName destination, MessagePayload payload)
    {
      _sendingLog.Info("Sending raw payload to " + destination);
      SendTransportMessage(new[] { destination }, CreateTransportMessage(Guid.Empty, payload));
    }

    public TransportMessage SendTransportMessage<T>(TransportMessage transportMessage)
    {
      return SendTransportMessage(_messageEndpointLookup.LookupEndpointsFor(typeof(T)), transportMessage);
    }

    public TransportMessage SendTransportMessage(IEnumerable<EndpointName> destinations, TransportMessage transportMessage)
    {
      foreach (EndpointName destination in destinations)
      {
        Send(destination, transportMessage);
      }
      return transportMessage;
    }

    public void Stop()
    {
      _log.Info("Stopping");
      _threads.Dispose();
    }

    public void Dispose()
    {
      Stop();
    }

    public IRequestReplyBuilder Request<T>(params T[] messages) where T : class, IMessage
    {
      return new RequestReplyBuilder(SendTransportMessage<T>(CreateTransportMessage(Guid.Empty, messages)), _asyncCallbackMap);
    }

    public void Reply<T>(params T[] messages) where T : class, IMessage
    {
      foreach (T message in messages) _sendingLog.Info("Replying " + message);
      TransportMessage transportMessage = CurrentMessageContext.Current;
      EndpointName returnAddress = transportMessage.ReturnAddress;
      SendTransportMessage(new[] { returnAddress }, CreateTransportMessage(transportMessage.ReturnCorrelationId, messages));
    }

    public void Publish<T>(params T[] messages) where T : class, IMessage
    {
      foreach (T message in messages) _sendingLog.Info("Publishing " + message);
      SendTransportMessage<T>(CreateTransportMessage(Guid.Empty, messages));
    }

    private void EndpointDispatcher(object obj)
    {
			if (obj == null)
			{
			  return;
			}
      TransportMessage transportMessage = (TransportMessage)obj;
      if (_messageFailureManager.SendToPoisonQueue(transportMessage.Id))
      {
        _poisonLog.Info("Poison " + transportMessage.ReturnAddress + " CorrelationId=" + transportMessage.CorrelationId + " Id=" + transportMessage.Id);
        _poison.Send(transportMessage);
        return;
      }
      try
      {
        using (CurrentMessageContext.Open(transportMessage))
        {
          _receivingLog.Info("Receiving " + transportMessage.ReturnAddress + " CorrelationId=" + transportMessage.CorrelationId + " Id=" + transportMessage.Id);
          IMessage[] messages = _transportMessageBodySerializer.Deserialize(transportMessage.Body);
          if (transportMessage.CorrelationId != Guid.Empty)
          {
            _asyncCallbackMap.InvokeAndRemove(transportMessage.CorrelationId, messages);
          }
          _dispatcher.Dispatch(messages);
        }
      }
      catch (Exception error)
      {
        _log.Error(error);
        _messageFailureManager.RecordFailure(transportMessage.Id, error);
        throw;
      }
    }

    private void Send(EndpointName destination, TransportMessage transportMessage)
    {
      IEndpoint endpoint = _endpointResolver.Resolve(destination);
      endpoint.Send(transportMessage);
    }

    private TransportMessage CreateTransportMessage<T>(Guid correlatedBy, params T[] messages) where T : class, IMessage
    {
      byte[] body = _transportMessageBodySerializer.Serialize(messages);
      return CreateTransportMessage(correlatedBy, new MessagePayload(body));
    }

    private TransportMessage CreateTransportMessage(Guid correlatedBy, MessagePayload payload)
    {
      return TransportMessage.For(_returnAddressProvider.GetReturnAddress(this.Address), correlatedBy,
        CurrentCorrelationContext.CurrentCorrelation,
        CurrentSagaContext.CurrentSagaId,
        payload.ToByteArray());
    }
  }
}
