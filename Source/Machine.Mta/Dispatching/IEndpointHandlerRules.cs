using System;
using System.Collections.Generic;

namespace Machine.Mta.Dispatching
{
  public interface IEndpointHandlerRules
  {
    void AddRule(EndpointAddress address, Func<Type, bool> rule);
    bool ApplyRules(EndpointAddress address, Type handlerType);
  }
}
