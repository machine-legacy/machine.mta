using System;
using System.Collections.Generic;

using MassTransit.ServiceBus.Internal;

namespace Machine.Mta.Minimalistic
{
  public interface IMessageBusFactory
  {
    IMessageBus CreateMessageBus(EndpointName listeningOnEndpointName, EndpointName poisonEndpointName);
  }
  public class MessageBusFactory : IMessageBusFactory
  {
    private readonly IEndpointResolver _endpointResolver;
    private readonly IMassTransitUriFactory _uriFactory;
    private readonly IMessageEndpointLookup _messageEndpointLookup;
    private readonly TransportMessageBodySerializer _transportMessageBodySerializer;
    private readonly MessageDispatcher _messageDispatcher;

    public MessageBusFactory(IEndpointResolver endpointResolver, IMassTransitUriFactory uriFactory, IMessageEndpointLookup messageEndpointLookup, TransportMessageBodySerializer transportMessageBodySerializer, MessageDispatcher messageDispatcher)
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
