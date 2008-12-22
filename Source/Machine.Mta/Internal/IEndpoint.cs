using System;
using System.Collections.Generic;

namespace Machine.Mta.Internal
{
  public interface IEndpoint
  {
    void Send(object message);
    object Receive(TimeSpan timeout);
  }
}
