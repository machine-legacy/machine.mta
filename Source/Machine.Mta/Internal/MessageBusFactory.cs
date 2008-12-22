using System;
using System.Collections.Generic;

namespace Machine.Mta.Internal
{
  public class MessageBusFactory : IMessageBusFactory
  {
    private readonly IEndpointResolver _endpointResolver;
    private readonly IMessageEndpointLookup _messageEndpointLookup;
    private readonly TransportMessageBodySerializer _transportMessageBodySerializer;
    private readonly MessageDispatcher _messageDispatcher;

    public MessageBusFactory(IEndpointResolver endpointResolver, IMessageEndpointLookup messageEndpointLookup, TransportMessageBodySerializer transportMessageBodySerializer, MessageDispatcher messageDispatcher)
    {
      _endpointResolver = endpointResolver;
      _messageEndpointLookup = messageEndpointLookup;
      _transportMessageBodySerializer = transportMessageBodySerializer;
      _messageDispatcher = messageDispatcher;
    }

    public IMessageBus CreateMessageBus(EndpointName listeningOnEndpointName, EndpointName poisonEndpointName)
    {
      return new MessageBus(_endpointResolver, _messageEndpointLookup, _transportMessageBodySerializer, _messageDispatcher, listeningOnEndpointName, poisonEndpointName);
    }
  }
}