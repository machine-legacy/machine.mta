using System;
using System.Collections.Generic;

namespace Machine.Mta.Endpoints
{
  public interface IEndpoint
  {
    void Send(TransportMessage transportMessage);
    TransportMessage Receive(TimeSpan timeout);
  }
}
