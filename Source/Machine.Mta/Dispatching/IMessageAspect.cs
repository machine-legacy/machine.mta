using System;
using System.Collections.Generic;

namespace Machine.Mta.Dispatching
{
  public interface IMessageAspect
  {
    void Continue(HandlerInvocation invocation);
  }
}