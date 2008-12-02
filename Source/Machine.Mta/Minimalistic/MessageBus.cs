using System;
using System.Collections.Generic;

using MassTransit.ServiceBus;
using MassTransit.ServiceBus.Internal;
using MassTransit.ServiceBus.Threading;

namespace Machine.Mta.Minimalistic
{
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
    }

    public void Send<T>(params T[] messages) where T : class, IMessage
    {
      SendTransportMessage<T>(CreateTransportMessage(Guid.Empty, messages));
    }

    public void Send<T>(EndpointName destination, params T[] messages) where T : class, IMessage
    {
      SendTransportMessage(new[] { destination }, CreateTransportMessage(Guid.Empty, messages));
    }

    public TransportMessage SendTransportMessage<T>(TransportMessage transportMessage)
    {
      return SendTransportMessage(_messageEndpointLookup.LookupEndpointsFor(typeof (T)), transportMessage);
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
      SendTransportMessage(new[] { returnAddress }, CreateTransportMessage(cmc.TransportMessage.Id, messages));
    }

    public void Publish<T>(params T[] messages) where T : class, IMessage
    {
      // Yes, this isn't really doing a Publish... See the commit message.
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
      try
      {
        using (CurrentMessageContext.Open(transportMessage))
        {
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
        _poison.Send(transportMessage);
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
      return new TransportMessage(this.Address, correlatedBy, payload.ToByteArray());
    }
  }
}
