using System;
using System.Collections.Generic;
using NServiceBus;

namespace Machine.Mta
{
  public interface IConsume<T> where T : IMessage
  {
    void Consume(T message);
  }
}
