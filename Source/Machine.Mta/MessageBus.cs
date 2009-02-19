using System;
using System.Collections.Generic;

using Machine.Mta.Dispatching;
using Machine.Mta.Endpoints;
using Machine.Utility.ThreadPool;
using Machine.Utility.ThreadPool.QueueStrategies;

namespace Machine.Mta
{
  public class MessageBus : IMessageBus
  {
    static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(MessageBus));
    readonly IMessageDestinations _messageDestinations;
    readonly IEndpointResolver _endpointResolver;
    readonly IEndpoint _listeningOn;
    readonly EndpointAddress _listeningOnEndpointAddress;
    readonly EndpointAddress _poisonEndpointAddress;
    readonly ThreadPool _threads;
    readonly TransportMessageBodySerializer _transportMessageBodySerializer;
    readonly AsyncCallbackMap _asyncCallbackMap;
    readonly ReturnAddressProvider _returnAddressProvider;
    readonly ITransactionManager _transactionManager;
    readonly MessageDispatchAttempter _messageDispatchAttempter;

    public MessageBus(IEndpointResolver endpointResolver, IMessageDestinations messageDestinations, TransportMessageBodySerializer transportMessageBodySerializer,
                      IMessageDispatcher dispatcher, EndpointAddress listeningOnEndpointAddress, EndpointAddress poisonEndpointAddress, ITransactionManager transactionManager,
                      ThreadPoolConfiguration threadPoolConfiguration)
    {
      _endpointResolver = endpointResolver;
      _transactionManager = transactionManager;
      _transportMessageBodySerializer = transportMessageBodySerializer;
      _messageDestinations = messageDestinations;
      _listeningOn = _endpointResolver.Resolve(listeningOnEndpointAddress);
      _listeningOnEndpointAddress = listeningOnEndpointAddress;
      _poisonEndpointAddress = poisonEndpointAddress;
      _messageDestinations = messageDestinations;
      _asyncCallbackMap = new AsyncCallbackMap();
      _returnAddressProvider = new ReturnAddressProvider();
      IEndpoint poison = _endpointResolver.Resolve(poisonEndpointAddress);
      MessageFailureManager messageFailureManager = new MessageFailureManager();
      _messageDispatchAttempter = new MessageDispatchAttempter(messageFailureManager, dispatcher, _transportMessageBodySerializer, _asyncCallbackMap, poison, this);
      _threads = new ThreadPool(threadPoolConfiguration, new SingleQueueStrategy(new EndpointQueue(_transactionManager, _listeningOn, _messageDispatchAttempter.AttemptDispatch)));
    }

    public MessageBus(IEndpointResolver endpointResolver, IMessageDestinations messageDestinations, TransportMessageBodySerializer transportMessageBodySerializer, IMessageDispatcher dispatcher, EndpointAddress listeningOnEndpointAddress, EndpointAddress poisonEndpointAddress, ITransactionManager transactionManager)
      : this(endpointResolver, messageDestinations, transportMessageBodySerializer, dispatcher, listeningOnEndpointAddress, poisonEndpointAddress, transactionManager, new ThreadPoolConfiguration(1, 1))
    {
    }

    public EndpointAddress PoisonAddress
    {
      get { return _poisonEndpointAddress; }
    }

    public EndpointAddress Address
    {
      get { return _listeningOnEndpointAddress; }
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
      Logging.Send(messages);
      SendTransportMessage<T>(CreateTransportMessage(Guid.Empty, false, messages));
    }

    public void Send<T>(EndpointAddress destination, params T[] messages) where T : class, IMessage
    {
      Logging.Send(destination, messages);
      SendTransportMessage(new[] { destination }, CreateTransportMessage(Guid.Empty, false, messages));
    }

    public void Send(EndpointAddress destination, MessagePayload payload)
    {
      Logging.SendMessagePayload(destination, payload);
      SendTransportMessage(new[] { destination }, CreateTransportMessage(Guid.Empty, payload, false));
    }

    public void SendLocal<T>(params T[] messages) where T : class, IMessage
    {
      Send(this.Address, messages);
    }

    public TransportMessage SendTransportMessage<T>(TransportMessage transportMessage)
    {
      return SendTransportMessage(_messageDestinations.LookupEndpointsFor(typeof(T)), transportMessage);
    }

    public TransportMessage SendTransportMessage(IEnumerable<EndpointAddress> destinations, TransportMessage transportMessage)
    {
      foreach (EndpointAddress destination in destinations)
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
      return new RequestReplyBuilder(SendTransportMessage<T>(CreateTransportMessage(Guid.Empty, false, messages)), _asyncCallbackMap);
    }

    public void Reply<T>(params T[] messages) where T : class, IMessage
    {
      Logging.Reply(messages);
      TransportMessage transportMessage = CurrentMessageContext.CurrentTransportMessage;
      EndpointAddress returnAddress = transportMessage.ReturnAddress;
      SendTransportMessage(new[] { returnAddress }, CreateTransportMessage(transportMessage.ReturnCorrelationId, true, messages));
    }

    public void Publish<T>(params T[] messages) where T : class, IMessage
    {
      Logging.Publish(messages);
      SendTransportMessage<T>(CreateTransportMessage(Guid.Empty, false, messages));
    }

    private void Send(EndpointAddress destination, TransportMessage transportMessage)
    {
      IEndpoint endpoint = _endpointResolver.Resolve(destination);
      endpoint.Send(transportMessage);
    }

    private TransportMessage CreateTransportMessage<T>(Guid correlatedBy, bool forReply, params T[] messages) where T : class, IMessage
    {
      MessagePayload payload = _transportMessageBodySerializer.Serialize(messages);
      return CreateTransportMessage(correlatedBy, payload, forReply);
    }

    private TransportMessage CreateTransportMessage(Guid correlatedBy, MessagePayload payload, bool forReply)
    {
      return TransportMessage.For(_returnAddressProvider.GetReturnAddress(this.Address), correlatedBy,
        CurrentCorrelationContext.CurrentCorrelation,
        CurrentSagaContext.CurrentSagaIds(forReply), payload);
    }
  }

  public class MessageDispatchAttempter
  {
    static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(MessageBus));
    readonly MessageFailureManager _messageFailureManager;
    readonly IMessageDispatcher _dispatcher;
    readonly TransportMessageBodySerializer _transportMessageBodySerializer;
    readonly AsyncCallbackMap _asyncCallbackMap;
    readonly IEndpoint _poison;
    readonly IMessageBus _bus;

    public MessageDispatchAttempter(MessageFailureManager messageFailureManager, IMessageDispatcher dispatcher, TransportMessageBodySerializer transportMessageBodySerializer, AsyncCallbackMap asyncCallbackMap, IEndpoint poison, IMessageBus bus)
    {
      _messageFailureManager = messageFailureManager;
      _bus = bus;
      _dispatcher = dispatcher;
      _transportMessageBodySerializer = transportMessageBodySerializer;
      _asyncCallbackMap = asyncCallbackMap;
      _poison = poison;
    }

    public void AttemptDispatch(object obj)
    {
      if (obj == null)
      {
        return;
      }
      TransportMessage transportMessage = (TransportMessage)obj;
      if (_messageFailureManager.SendToPoisonQueue(transportMessage.Id))
      {
        Logging.Poison(transportMessage);
        _poison.Send(transportMessage);
        return;
      }
      try
      {
        using (CurrentMessageBus.Open(_bus))
        {
          using (CurrentMessageContext.Open(transportMessage))
          {
            Logging.Received(transportMessage);
            IMessage[] messages = _transportMessageBodySerializer.Deserialize(transportMessage.Body);
            if (transportMessage.CorrelationId != Guid.Empty)
            {
              _asyncCallbackMap.InvokeAndRemove(transportMessage.CorrelationId, messages);
            }
            _dispatcher.Dispatch(messages);
          }
        }
      }
      catch (Exception error)
      {
        _log.Error(error);
        _messageFailureManager.RecordFailure(transportMessage.Id, error);
        throw;
      }
    }
  }
}
