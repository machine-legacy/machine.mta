using System;
using System.Collections.Generic;

using Machine.Mta.Internal;

namespace Machine.Mta.Transports.Msmq
{
  public class MsmqEndpointFactory : IEndpointFactory
  {
    public IEndpoint CreateEndpoint(EndpointName name)
    {
      return new MsmqEndpoint(name);
    }
  }
}
