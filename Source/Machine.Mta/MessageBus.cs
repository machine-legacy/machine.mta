using System;
using System.Collections.Generic;
using System.Linq;

using Machine.Mta.Dispatching;
using Machine.Mta.Endpoints;
using Machine.Utility.ThreadPool;

namespace Machine.Mta
{
  public class MessageBus : IMessageBus
  {
    static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(MessageBus));
    readonly IMessageDestinations _messageDestinations;
    readonly IEndpointResolver _endpointResolver;
    readonly IEndpoint _listeningOn;
    readonly EndpointAddress _listeningOnAddress;
    readonly EndpointAddress _poisonEndpointAddress;
    readonly AbstractThreadPool _threads;
    readonly TransportMessageBodySerializer _transportMessageBodySerializer;
    readonly AsyncCallbackMap _asyncCallbackMap;
    readonly ReturnAddressProvider _returnAddressProvider;
    readonly ITransactionManager _transactionManager;
    readonly MessageDispatchAttempter _messageDispatchAttempter;

    public MessageBus(IEndpointResolver endpointResolver, IMessageDestinations messageDestinations, TransportMessageBodySerializer transportMessageBodySerializer,
                      IMessageDispatcher dispatcher, EndpointAddress listeningOnEndpointAddress, EndpointAddress poisonEndpointAddress, ITransactionManager transactionManager,
                      IMessageFailureManager messageFailureManager, ThreadPoolConfiguration threadPoolConfiguration)
    {
      _endpointResolver = endpointResolver;
      _transactionManager = transactionManager;
      _transportMessageBodySerializer = transportMessageBodySerializer;
      _messageDestinations = messageDestinations;
      _listeningOnAddress = listeningOnEndpointAddress;
      _poisonEndpointAddress = poisonEndpointAddress;
      _messageDestinations = messageDestinations;
      _asyncCallbackMap = new AsyncCallbackMap();
      _returnAddressProvider = new ReturnAddressProvider();
      _listeningOn = _endpointResolver.Resolve(listeningOnEndpointAddress);
      IEndpoint poison = _endpointResolver.Resolve(poisonEndpointAddress);
      _messageDispatchAttempter = new MessageDispatchAttempter(dispatcher, _transportMessageBodySerializer, _asyncCallbackMap);
      _threads = new WorkerFactoryThreadPool(threadPoolConfiguration, new WorkerFactory(this, _transactionManager, messageFailureManager, _listeningOn, poison, _messageDispatchAttempter.AttemptDispatch));
    }

    public EndpointAddress PoisonAddress
    {
      get { return _poisonEndpointAddress; }
    }

    public EndpointAddress Address
    {
      get { return _listeningOnAddress; }
    }

    public void ChangeThreadPoolConfiguration(ThreadPoolConfiguration configuration)
    {
      _threads.ChangeConfiguration(configuration);
    }

    public void Start()
    {
      _log.Info("Starting " + _listeningOnAddress);
      _threads.Start();
    }

    public void Send<T>(params T[] messages) where T : class, IMessage
    {
      Logging.Send(messages);
      RouteTransportMessage<T>(CreateTransportMessage(null, false, messages));
    }

    public void Send<T>(EndpointAddress destination, params T[] messages) where T : class, IMessage
    {
      Logging.Send(destination, messages);
      SendTransportMessageOnlyTo(new[] { destination }, CreateTransportMessage(null, false, messages));
    }

    public void Send(EndpointAddress destination, MessagePayload payload)
    {
      Logging.SendMessagePayload(destination, payload);
      SendTransportMessageOnlyTo(new[] { destination }, CreateTransportMessage(null, payload, false));
    }

    public void SendLocal<T>(params T[] messages) where T : class, IMessage
    {
      Send(this.Address, messages);
    }

    public TransportMessage RouteTransportMessage<T>(TransportMessage transportMessage, params EndpointAddress[] destinations)
    {
      IEnumerable<EndpointAddress> allDestinations = _messageDestinations.LookupEndpointsFor(typeof(T), true).Union(destinations);
      return SendTransportMessageOnlyTo(allDestinations, transportMessage);
    }

    public TransportMessage SendTransportMessageOnlyTo(IEnumerable<EndpointAddress> destinations, TransportMessage transportMessage)
    {
      foreach (EndpointAddress destination in destinations.Distinct())
      {
        Send(destination, transportMessage);
      }
      return transportMessage;
    }

    public void Stop()
    {
      _log.Info("Stopping " + _listeningOnAddress);
      _threads.Dispose();
    }

    public void Dispose()
    {
      Stop();
    }

    public IRequestReplyBuilder Request<T>(params T[] messages) where T : class, IMessage
    {
      return new RequestReplyBuilder(CreateTransportMessage(null, false, messages), (x) => RouteTransportMessage<T>(x), _asyncCallbackMap);
    }

    public IRequestReplyBuilder Request<T>(string correlationId, params T[] messages) where T : class, IMessage
    {
      return new RequestReplyBuilder(CreateTransportMessage(correlationId, false, messages), (x) => RouteTransportMessage<T>(x), _asyncCallbackMap);
    }

    public void Reply<T>(params T[] messages) where T : class, IMessage
    {
      Reply(CurrentMessageContext.Current.ReturnAddress, CurrentMessageContext.Current.CorrelationId, messages);
    }

    public void Reply<T>(EndpointAddress destination, string correlationId, params T[] messages) where T : class, IMessage
    {
      Logging.Reply(messages);
      SendTransportMessageOnlyTo(new[] { destination }, CreateTransportMessage(correlationId, true, messages));
    }

    public void Reply<T>(string correlationId, params T[] messages) where T : class, IMessage
    {
      Reply(CurrentMessageContext.Current.ReturnAddress, correlationId, messages);
    }

    public void Publish<T>(params T[] messages) where T : class, IMessage
    {
      Logging.Publish(messages);
      RouteTransportMessage<T>(CreateTransportMessage(null, false, messages));
    }

    public void PublishAndReplyTo<T>(EndpointAddress destination, string correlationId, params T[] messages) where T : class, IMessage
    {
      Logging.Reply(messages);
      RouteTransportMessage<T>(CreateTransportMessage(correlationId, true, messages), destination);
    }

    public void PublishAndReply<T>(params T[] messages) where T : class, IMessage
    {
      Logging.Publish(messages);
      RouteTransportMessage<T>(CreateTransportMessage(null, false, messages), CurrentMessageContext.Current.ReturnAddress);
    }

    public void PublishAndReply<T>(string correlationId, params T[] messages) where T : class, IMessage
    {
      Logging.Publish(messages);
      RouteTransportMessage<T>(CreateTransportMessage(correlationId, false, messages), CurrentMessageContext.Current.ReturnAddress);
    }

    private void Send(EndpointAddress destination, TransportMessage transportMessage)
    {
      IEndpoint endpoint = _endpointResolver.Resolve(destination);
      endpoint.Send(transportMessage);
    }

    private TransportMessage CreateTransportMessage<T>(string correlationId, bool forReply, params T[] messages) where T : class, IMessage
    {
      MessagePayload payload = _transportMessageBodySerializer.Serialize(messages);
      return CreateTransportMessage(correlationId, payload, forReply);
    }

    private TransportMessage CreateTransportMessage(string correlationId, MessagePayload payload, bool forReply)
    {
      return TransportMessage.For(_returnAddressProvider.GetReturnAddress(this.Address), correlationId,
        CurrentSagaContext.CurrentSagaIds(forReply), payload);
    }
  }

  public class MessageDispatchAttempter
  {
    static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(MessageBus));
    readonly IMessageDispatcher _dispatcher;
    readonly TransportMessageBodySerializer _transportMessageBodySerializer;
    readonly AsyncCallbackMap _asyncCallbackMap;

    public MessageDispatchAttempter(IMessageDispatcher dispatcher, TransportMessageBodySerializer transportMessageBodySerializer, AsyncCallbackMap asyncCallbackMap)
    {
      _dispatcher = dispatcher;
      _transportMessageBodySerializer = transportMessageBodySerializer;
      _asyncCallbackMap = asyncCallbackMap;
    }

    public void AttemptDispatch(TransportMessage transportMessage)
    {
      Logging.Received(transportMessage);
      IMessage[] messages = _transportMessageBodySerializer.Deserialize(transportMessage.Body);
      if (transportMessage.CorrelationId != null)
      {
        _asyncCallbackMap.InvokeAndRemove(transportMessage.CorrelationId, messages);
      }
      _dispatcher.Dispatch(messages);
    }
  }
}
