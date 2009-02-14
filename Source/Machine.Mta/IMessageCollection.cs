using System;
using System.Collections.Generic;

namespace Machine.Mta
{
  public interface IMessageCollection
  {
    IEnumerable<Type> MessageTypes();
  }
}
