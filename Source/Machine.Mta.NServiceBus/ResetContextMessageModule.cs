using System;
using System.Collections.Generic;
using NServiceBus;

namespace Machine.Mta
{
  public class ResetContextMessageModule : IMessageModule
  {
    public void HandleBeginMessage()
    {
      CurrentSagaIds.Reset();
    }

    public void HandleEndMessage()
    {
    }

    public void HandleError(Exception error)
    {
    }
  }
}
