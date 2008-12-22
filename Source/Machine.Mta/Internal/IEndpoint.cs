using System;
using System.Collections.Generic;

namespace Machine.Mta.Internal
{
  public interface IEndpoint
  {
    void Send(TransportMessage transportMessage);
    TransportMessage Receive(TimeSpan timeout);
  }
}
