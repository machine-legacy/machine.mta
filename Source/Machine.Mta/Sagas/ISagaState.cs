using System;
using System.Collections.Generic;

namespace Machine.Mta.Sagas
{
  public interface ISagaState
  {
    Guid SagaId { get; }
    bool IsSagaComplete { get; }
  }
}
