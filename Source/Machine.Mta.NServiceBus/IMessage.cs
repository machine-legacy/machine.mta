using System;
using System.Collections.Generic;

namespace Machine.Mta
{
  public interface INsbMessage : NServiceBus.IMessage, IMessage
  {
  }
}
