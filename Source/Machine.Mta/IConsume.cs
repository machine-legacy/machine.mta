using System;
using System.Collections.Generic;

namespace Machine.Mta
{
  public interface IConsume<T> where T : class, IMessage
  {
    void Consume(T message);
  }
}
