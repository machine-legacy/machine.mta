using System;
using System.Collections.Generic;
using System.Messaging;

using Machine.Mta.Endpoints;

namespace Machine.Mta.Transports.Msmq
{
  public class MsmqEndpointFactory : IEndpointFactory
  {
    readonly MsmqTransactionManager _transactionManager;

    public MsmqEndpointFactory(MsmqTransactionManager transactionManager)
    {
      _transactionManager = transactionManager;
    }

    public IEndpoint CreateEndpoint(EndpointAddress address)
    {
      MessageQueue queue = new MessageQueue(address.ToPath(), QueueAccessMode.SendAndReceive);
      return new MsmqEndpoint(address, queue, _transactionManager);
    }
  }
  public static class EndpointAddressHelpers
  {
    public static string ToPath(this EndpointAddress address)
    {
      return String.Format(@"FormatName:DIRECT=OS:{0}\Private$\{1}", address.Host, address.Name);
    }
  }
}
