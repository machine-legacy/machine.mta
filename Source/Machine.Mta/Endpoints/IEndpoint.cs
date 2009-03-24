using System;
using System.Collections.Generic;

namespace Machine.Mta.Endpoints
{
  public interface IEndpoint
  {
    void Send(TransportMessage transportMessage);
    bool HasAnyPendingMessages(TimeSpan timeout);
    TransportMessage Receive(TimeSpan timeout);
  }
}
