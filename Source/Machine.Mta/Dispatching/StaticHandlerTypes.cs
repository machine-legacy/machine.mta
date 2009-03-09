using System;
using System.Collections.Generic;

namespace Machine.Mta.Dispatching
{
  public class StaticHandlerTypes : IProvideHandlerTypes
  {
    readonly Type[] _types;

    public StaticHandlerTypes(params Type[] types)
    {
      _types = types;
    }

    public IEnumerable<Type> HandlerTypes()
    {
      return _types;
    }
  }
}
