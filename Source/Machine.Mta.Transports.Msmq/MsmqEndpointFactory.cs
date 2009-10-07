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
      MessageQueue queue = new MessageQueue(address.ToNameAndHost().ToMsmqPath(), QueueAccessMode.SendAndReceive);
      MessagePropertyFilter filter = new MessagePropertyFilter();
      filter.SetAll();
      queue.MessageReadPropertyFilter = filter;
      return new MsmqEndpoint(address, queue, _transactionManager);
    }
  }
  
  public static class EndpointAddressHelpers
  {
    public static string ToMsmqPath(this NameAndHostAddress address)
    {
      return String.Format(@"FormatName:DIRECT=OS:{0}\Private$\{1}", address.Host, address.Name);
    }

    public static EndpointAddress ToAddress(this MessageQueue queue)
    {
      return queue.ToNameAndHostAddress().ToAddress();
    }

    public static NameAndHostAddress ToNameAndHostAddress(this MessageQueue queue)
    {
      try
      {
        if (queue == null) return null;
        string[] tokens = queue.Path.Split('\\');
        string queueName = tokens[tokens.Length - 1];
        string[] firstHalfTokens = tokens[0].Split(':');
        return NameAndHostAddress.ForRemoteQueue(firstHalfTokens[firstHalfTokens.Length - 1], queueName);
      }
      catch (Exception error)
      {
        return NameAndHostAddress.Null;
      }
    }
  }
}
