using System;
using System.Collections.Generic;

using MassTransit.Internal;

namespace Machine.Mta.Internal
{
  public class MessageBusFactory : IMessageBusFactory
  {
    private readonly IEndpointResolver _endpointResolver;
    private readonly IMtaUriFactory _uriFactory;
    private readonly IMessageEndpointLookup _messageEndpointLookup;
    private readonly TransportMessageBodySerializer _transportMessageBodySerializer;
    private readonly MessageDispatcher _messageDispatcher;

    public MessageBusFactory(IEndpointResolver endpointResolver, IMtaUriFactory uriFactory, IMessageEndpointLookup messageEndpointLookup, TransportMessageBodySerializer transportMessageBodySerializer, MessageDispatcher messageDispatcher)
    {
      _endpointResolver = endpointResolver;
      _uriFactory = uriFactory;
      _messageEndpointLookup = messageEndpointLookup;
      _transportMessageBodySerializer = transportMessageBodySerializer;
      _messageDispatcher = messageDispatcher;
    }

    #region IMessageBusFactory Members
    public IMessageBus CreateMessageBus(EndpointName listeningOnEndpointName, EndpointName poisonEndpointName)
    {
      return new MessageBus(_endpointResolver, _uriFactory, _messageEndpointLookup, _transportMessageBodySerializer, _messageDispatcher, listeningOnEndpointName, poisonEndpointName);
    }
    #endregion
  }
}