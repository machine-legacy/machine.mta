using System;
using System.Collections.Generic;

using Machine.Container.Services;
using Machine.Mta.Dispatching;
using Machine.Mta.Endpoints;

namespace Machine.Mta
{
  public class MessageBusFactory : IMessageBusFactory
  {
    readonly IEndpointResolver _endpointResolver;
    readonly IMessageDestinations _messageDestinations;
    readonly TransportMessageBodySerializer _transportMessageBodySerializer;
    readonly ITransactionManager _transactionManager;
    readonly IMachineContainer _container;
    readonly IMessageAspectsProvider _messageAspectsProvider;

    public MessageBusFactory(IEndpointResolver endpointResolver, IMessageDestinations messageDestinations, TransportMessageBodySerializer transportMessageBodySerializer, ITransactionManager transactionManager, IMachineContainer container, IMessageAspectsProvider messageAspectsProvider)
    {
      _endpointResolver = endpointResolver;
      _messageAspectsProvider = messageAspectsProvider;
      _container = container;
      _transactionManager = transactionManager;
      _messageDestinations = messageDestinations;
      _transportMessageBodySerializer = transportMessageBodySerializer;
    }

    public IMessageBus CreateMessageBus(EndpointAddress listeningOnEndpointAddress, EndpointAddress poisonEndpointAddress, IProvideHandlerTypes handlerTypes)
    {
      MessageDispatcher dispatcher = new MessageDispatcher(_container, _messageAspectsProvider, handlerTypes);
      return new MessageBus(_endpointResolver, _messageDestinations, _transportMessageBodySerializer, dispatcher, listeningOnEndpointAddress, poisonEndpointAddress, _transactionManager);
    }
  }
}