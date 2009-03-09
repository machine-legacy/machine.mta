using System;
using System.Collections.Generic;

namespace Machine.Mta.Dispatching
{
  public interface IProvideHandlerTypes
  {
    IEnumerable<Type> HandlerTypes();
  }
}
