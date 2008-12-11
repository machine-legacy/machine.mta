using System;
using System.Collections.Generic;

namespace Machine.Mta
{
  public interface IConsume<T> : MassTransit.Consumes<T>.All where T : class, IMessage
  {
  }
}
