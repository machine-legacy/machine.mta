using System;
using System.Collections.Generic;

namespace Machine.Mta
{
  public interface IInspectBusTypes
  {
    bool IsConsumer(Type type);
    bool IsSagaConsumer(Type type);
  }
}
