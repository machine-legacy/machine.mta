using System;
using System.Collections.Generic;

namespace Machine.Mta.Dispatching
{
  public interface IMessageModule
  {
    void Begin();
    void End();
  }
}
