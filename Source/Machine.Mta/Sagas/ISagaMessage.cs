using System;
using System.Collections.Generic;

using MassTransit.ServiceBus;

namespace Machine.Mta.Sagas
{
  public interface ISagaMessage : IMessage
  {
    Guid SagaId { get; set; }
  }
  public interface ISagaStartedBy<T> : Consumes<T>.All where T : class, IMessage
  {
  }
}
