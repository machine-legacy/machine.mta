using System;
using System.Collections.Generic;

using Machine.Mta.Dispatching;
using Machine.Mta.Endpoints;

namespace Machine.Mta
{
  public class MessageBusFactory : IMessageBusFactory
  {
    private readonly IEndpointResolver _endpointResolver;
    private readonly IMessageEndpointLookup _messageEndpointLookup;
    private readonly TransportMessageBodySerializer _transportMessageBodySerializer;
    private readonly MessageDispatcher _messageDispatcher;
    private readonly ITransactionManager _transactionManager;

    public MessageBusFactory(IEndpointResolver endpointResolver, IMessageEndpointLookup messageEndpointLookup, TransportMessageBodySerializer transportMessageBodySerializer, MessageDispatcher messageDispatcher, ITransactionManager transactionManager)
    {
      _endpointResolver = endpointResolver;
      _transactionManager = transactionManager;
      _messageEndpointLookup = messageEndpointLookup;
      _transportMessageBodySerializer = transportMessageBodySerializer;
      _messageDispatcher = messageDispatcher;
    }

    public IMessageBus CreateMessageBus(EndpointAddress listeningOnEndpointAddress, EndpointAddress poisonEndpointAddress)
    {
      return new MessageBus(_endpointResolver, _messageEndpointLookup, _transportMessageBodySerializer, _messageDispatcher, listeningOnEndpointAddress, poisonEndpointAddress, _transactionManager);
    }
  }
}