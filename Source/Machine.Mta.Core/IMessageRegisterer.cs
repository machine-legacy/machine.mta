using System;
using System.Collections.Generic;

namespace Machine.Mta
{
  public interface IMessageRegisterer
  {
    void AddMessageTypes(params Type[] types);
    void AddMessageTypes(IEnumerable<Type> types);
  }
}
