using System;
using System.Collections.Generic;

namespace Machine.Mta.Dispatching
{
  public interface IMessageDispatcher
  {
    void Dispatch(IMessage[] messages);
  }
}
