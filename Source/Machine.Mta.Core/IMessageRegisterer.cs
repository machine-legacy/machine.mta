using System;
using System.Collections.Generic;

namespace Machine.Mta
{
  public interface IMessageRegisterer
  {
    void AddMessageTypes(IEnumerable<Type> types);
    IEnumerable<Type> MessageTypes { get; }
  }
}
