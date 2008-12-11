using System;
using System.Collections.Generic;

using MassTransit;
using MassTransit.Internal;
using MassTransit.Threading;

namespace Machine.Mta.Minimalistic
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
    private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(MessageBus));
    private readonly IMtaUriFactory _uriFactory;
    private readonly IMessageEndpointLookup _messageEndpointLookup;
    private readonly IEndpointResolver _endpointResolver;
    private readonly IMessageDispatcher _dispatcher;
    private readonly IEndpoint _listeningOn;
    private readonly IEndpoint _poison;
    private readonly EndpointName _listeningOnEndpointName;
    private readonly EndpointName _poisonEndpointName;
    private readonly ResourceThreadPool<IEndpoint, object> _threads;
    private readonly TransportMessageBodySerializer _transportMessageBodySerializer;
    private readonly AsyncCallbackMap _asyncCallbackMap;
    private readonly MessageFailureManager _messageFailureManager;
    private readonly ReturnAddressProvider _returnAddressProvider;

    public MessageBus(IEndpointResolver endpointResolver, IMtaUriFactory uriFactory, IMessageEndpointLookup messageEndpointLookup, TransportMessageBodySerializer transportMessageBodySerializer, IMessageDispatcher dispatcher, EndpointName listeningOnEndpointName, EndpointName poisonEndpointName)
    {
      _endpointResolver = endpointResolver;
      _dispatcher = dispatcher;
      _transportMessageBodySerializer = transportMessageBodySerializer;
      _messageEndpointLookup = messageEndpointLookup;
      _uriFactory = uriFactory;
      _listeningOn = _endpointResolver.Resolve(_uriFactory.CreateUri(listeningOnEndpointName));
      _poison = _endpointResolver.Resolve(_uriFactory.CreateUri(poisonEndpointName));
      _listeningOnEndpointName = listeningOnEndpointName;
      _poisonEndpointName = poisonEndpointName;
      _messageEndpointLookup = messageEndpointLookup;
      _threads = new ResourceThreadPool<IEndpoint, object>(_listeningOn, EndpointReader, EndpointDispatcher, 10, 1, 1);
      _asyncCallbackMap = new AsyncCallbackMap();
      _messageFailureManager = new MessageFailureManager();
      _returnAddressProvider = new ReturnAddressProvider();
    }

    public EndpointName PoisonAddress
    {
      get { return _poisonEndpointName; }
    }

    public EndpointName Address
    {
      get { return _listeningOnEndpointName; }
    }

    public void Start()
    {
      _log.Info("Starting");
      _threads.WakeUp();
    }

    public void Send<T>(params T[] messages) where T : class, IMessage
    {
      SendTransportMessage<T>(CreateTransportMessage(Guid.Empty, messages));
    }

    public void Send<T>(EndpointName destination, params T[] messages) where T : class, IMessage
    {
      SendTransportMessage(new[] { destination }, CreateTransportMessage(Guid.Empty, messages));
    }

    public void Send(EndpointName destination, MessagePayload payload)
    {
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
      CurrentMessageContext cmc = CurrentMessageContext.Current;
      EndpointName returnAddress = cmc.TransportMessage.ReturnAddress;
      SendTransportMessage(new[] { returnAddress }, CreateTransportMessage(cmc.TransportMessage.ReturnCorrelationId, messages));
    }

    public void Publish<T>(params T[] messages) where T : class, IMessage
    {
      SendTransportMessage<T>(CreateTransportMessage(Guid.Empty, messages));
    }

    private static object EndpointReader(IEndpoint resource)
    {
      try
      {
        return resource.Receive(TimeSpan.FromSeconds(3), Accept);
      }
      catch (Exception error)
      {
        _log.Error(error);
        return null;
      }
    }

    private static bool Accept(object obj)
    {
      return obj is TransportMessage;
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
        _log.Info("Poison " + transportMessage.ReturnAddress + " CorrelationId=" + transportMessage.CorrelationId + " Id=" + transportMessage.Id);
        _poison.Send(transportMessage);
        return;
      }
      try
      {
        using (CurrentMessageContext.Open(transportMessage))
        {
          _log.Info("Receiving " + transportMessage.ReturnAddress + " CorrelationId=" + transportMessage.CorrelationId + " Id=" + transportMessage.Id);
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
      Uri uri = _uriFactory.CreateUri(destination);
      IEndpoint endpoint = _endpointResolver.Resolve(uri);
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
