using System;
using System.Collections.Generic;

namespace Machine.Mta.Dispatching
{
  public interface IMessageAspectsProvider
  {
    Queue<IMessageAspect> DefaultAspects();
  }
}