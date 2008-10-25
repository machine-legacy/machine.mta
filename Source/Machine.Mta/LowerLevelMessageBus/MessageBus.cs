using System;
using System.Collections.Generic;
using System.Threading;
using MassTransit.ServiceBus;
using MassTransit.ServiceBus.Internal;
using MassTransit.ServiceBus.Threading;

namespace Machine.Mta.LowerLevelMessageBus
{
  public class MessageBus : IMessageBus
  {
    private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(IMessageBus));
    private readonly IMassTransitUriFactory _uriFactory;
    private readonly IMessageEndpointLookup _messageEndpointLookup;
    private readonly IEndpointResolver _endpointResolver;
    private readonly IEndpoint _listeningOn;
    private readonly IEndpoint _poison;
    private readonly EndpointName _listeningOnEndpointName;
    private readonly EndpointName _poisonEndpointName;
    private readonly ResourceThreadPool<IEndpoint, object> _threads;
    private readonly TransportMessageBodySerializer _transportMessageBodySerializer;
    private readonly MessageDispatcher _dispatcher;
    private readonly AsyncCallbackMap _asyncCallbackMap;

    public MessageBus(IEndpointResolver endpointResolver, IMassTransitUriFactory uriFactory, IMessageEndpointLookup messageEndpointLookup, TransportMessageBodySerializer transportMessageBodySerializer, MessageDispatcher dispatcher, EndpointName listeningOnEndpointName, EndpointName poisonEndpointName)
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
    }

    public EndpointName Address
    {
      get { return _listeningOnEndpointName; }
    }

    public void Start()
    {
    }

    public void Send<T>(params T[] messages) where T : class, IMessage
    {
      CreateAndSend(Guid.Empty, messages);
    }

    public void Send<T>(EndpointName destination, params T[] messages) where T : class, IMessage
    {
      CreateAndSend(new[] { destination }, Guid.Empty, messages);
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
      TransportMessage transportMessage = new TransportMessage(this.Address, correlatedBy, body);
      return transportMessage;
    }

    private TransportMessage CreateAndSend<T>(Guid correlatedBy, params T[] messages) where T : class, IMessage
    {
      return CreateAndSend(_messageEndpointLookup.LookupEndpointFor(typeof(T)), correlatedBy, messages);
    }

    private TransportMessage CreateAndSend<T>(IEnumerable<EndpointName> destinations, Guid correlatedBy, params T[] messages) where T : class, IMessage
    {
      TransportMessage transportMessage = CreateTransportMessage<T>(correlatedBy, messages);
      foreach (EndpointName destination in destinations)
      {
        Send(destination, transportMessage);
      }
      return transportMessage;
    }

    public void Stop()
    {
      _threads.Dispose();
    }

    public void Dispose()
    {
      Stop();
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
      try
      {
        using (CurrentMessageContext.Open(transportMessage))
        {
          ICollection<IMessage> messages = _transportMessageBodySerializer.Deserialize(transportMessage.Body);
          if (transportMessage.CorrelationId != Guid.Empty)
          {
            _asyncCallbackMap.InvokeAndRemove(transportMessage.CorrelationId);
          }
          foreach (IMessage message in messages)
          {
            _dispatcher.Dispatch(message);
          }
        }
      }
      catch (Exception error)
      {
        _log.Error(error);
        _poison.Send(transportMessage);
      }
    }

    public IRequestReplyBuilder Request<T>(params T[] messages) where T : class, IMessage
    {
      return new RequestReplyBuilder(CreateAndSend(Guid.Empty, messages), _asyncCallbackMap);
    }

    public void Reply<T>(params T[] messages) where T : class, IMessage
    {
      CurrentMessageContext cmc = CurrentMessageContext.Current;
      EndpointName returnAddress = cmc.TransportMessage.ReturnAddress;
      CreateAndSend(new[] { returnAddress }, cmc.TransportMessage.Id, messages);
    }

    public void Publish<T>(params T[] messages) where T : class, IMessage
    {
      // Yes, this isn't really doing a Publish... See the commit message.
      CreateAndSend(Guid.Empty, messages);
    }
  }
  
  public class RequestReplyBuilder : IRequestReplyBuilder
  {
    private readonly TransportMessage _request;
    private readonly AsyncCallbackMap _asyncCallbackMap;

    public RequestReplyBuilder(TransportMessage request, AsyncCallbackMap asyncCallbackMap)
    {
      _request = request;
      _asyncCallbackMap = asyncCallbackMap;
    }

    #region IRequestReplyBuilder Members
    public void OnReply(AsyncCallback callback, object state)
    {
      _asyncCallbackMap.Add(_request.Id, callback, state);
    }

    public void OnReply(AsyncCallback callback)
    {
      OnReply(callback, null);
    }
    #endregion
  }

  public class MessageBusAsyncResult : IAsyncResult
  {
    private readonly AsyncCallback _callback;
    private readonly object _state;
    private readonly ManualResetEvent _waitHandle;
    private volatile bool _completed;

    public object AsyncState
    {
      get { return _state; }
    }

    public WaitHandle AsyncWaitHandle
    {
      get { return _waitHandle; }
    }

    public bool CompletedSynchronously
    {
      get { return false; }
    }

    public bool IsCompleted
    {
      get { return _completed; }
    }

    public MessageBusAsyncResult(AsyncCallback callback, object state)
    {
      _callback = callback;
      _state = state;
      _waitHandle = new ManualResetEvent(false);
    }

    public void Complete()
    {
      _completed = true;
      _waitHandle.Set();
      if (_callback != null)
      {
        _callback(this);
      }
    }
  }

  public class AsyncCallbackMap
  {
    private readonly Dictionary<Guid, MessageBusAsyncResult> _map = new Dictionary<Guid, MessageBusAsyncResult>();

    public void Add(Guid id, AsyncCallback callback, object state)
    {
      lock (_map)
      {
        _map[id] = new MessageBusAsyncResult(callback, state);
      }
    }

    public void InvokeAndRemove(Guid id)
    {
      MessageBusAsyncResult ar;
      lock (_map)
      {
        if (!_map.TryGetValue(id, out ar))
        {
          return;
        }
        _map.Remove(id);
      }
      ar.Complete();
    }
  }
}
