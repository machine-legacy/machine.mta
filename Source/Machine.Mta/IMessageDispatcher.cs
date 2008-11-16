using System;
using System.Collections.Generic;

namespace Machine.Mta
{
  public interface IMessageDispatcher
  {
    void Dispatch(IMessage[] messages);
  }
}
