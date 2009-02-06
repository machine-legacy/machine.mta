using System;
using System.Collections.Generic;

using Machine.Mta.Dispatching;
using Machine.Mta.Endpoints;

namespace Machine.Mta
{
  public class MessageBusFactory : IMessageBusFactory
  {
    private readonly IEndpointResolver _endpointResolver;
    private readonly IMessageDestinations _messageDestinations;
    private readonly TransportMessageBodySerializer _transportMessageBodySerializer;
    private readonly MessageDispatcher _messageDispatcher;
    private readonly ITransactionManager _transactionManager;

    public MessageBusFactory(IEndpointResolver endpointResolver, IMessageDestinations messageDestinations, TransportMessageBodySerializer transportMessageBodySerializer, MessageDispatcher messageDispatcher, ITransactionManager transactionManager)
    {
      _endpointResolver = endpointResolver;
      _transactionManager = transactionManager;
      _messageDestinations = messageDestinations;
      _transportMessageBodySerializer = transportMessageBodySerializer;
      _messageDispatcher = messageDispatcher;
    }

    public IMessageBus CreateMessageBus(EndpointAddress listeningOnEndpointAddress, EndpointAddress poisonEndpointAddress)
    {
      return new MessageBus(_endpointResolver, _messageDestinations, _transportMessageBodySerializer, _messageDispatcher, listeningOnEndpointAddress, poisonEndpointAddress, _transactionManager);
    }
  }
}