using System;
using System.Collections.Generic;

using Machine.Mta.Internal;

namespace Machine.Mta.Transports.Msmq
{
  public class MsmqEndpoint : IEndpoint
  {
    readonly EndpointName _name;

    public MsmqEndpoint(EndpointName name)
    {
      _name = name;
    }

    public void Send(object message)
    {
      throw new System.NotImplementedException();
    }

    public object Receive(TimeSpan timeout)
    {
      throw new System.NotImplementedException();
    }
  }
}
