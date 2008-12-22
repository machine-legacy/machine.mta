using System;
using System.Collections.Generic;
using System.Messaging;

using Machine.Mta.Internal;

namespace Machine.Mta.Transports.Msmq
{
  public class MsmqEndpointFactory : IEndpointFactory
  {
    public IEndpoint CreateEndpoint(EndpointName name)
    {
      MessageQueue queue = new MessageQueue(name.ToPath(), QueueAccessMode.SendAndReceive);
      return new MsmqEndpoint(name, queue);
    }
  }
  public static class EndpointNameHelpers
  {
    public static string ToPath(this EndpointName name)
    {
      return String.Format(@"FormatName:DIRECT=OS:{0}\Private$\{1}", name.Address, name.Name);
    }
  }
}
