using System;
using System.Collections.Generic;

namespace Machine.Mta.Sagas
{
  public interface ISagaStateRepository<T> where T : class, ISagaState
  {
    T FindSagaState(Guid sagaId);
    void Save(T sagaState);
    void Delete(T sagaState);
  }
}
