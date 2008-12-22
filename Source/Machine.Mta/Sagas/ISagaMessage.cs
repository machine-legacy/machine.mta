using System;
using System.Collections.Generic;

namespace Machine.Mta.Sagas
{
  public interface ISagaMessage : IMessage
  {
    Guid SagaId { get; set; }
  }
  public interface ISagaStartedBy<T> : IConsume<T> where T : class, IMessage
  {
  }
}
