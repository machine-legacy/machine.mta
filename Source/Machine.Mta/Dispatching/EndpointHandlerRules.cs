using System;
using System.Collections.Generic;
using System.Threading;

using Machine.Core.Utility;

namespace Machine.Mta.Dispatching
{
  public class EndpointHandlerRules : IEndpointHandlerRules
  {
    readonly ReaderWriterLock _lock = new ReaderWriterLock();
    readonly Dictionary<EndpointAddress, Func<Type, bool>> _rules = new Dictionary<EndpointAddress, Func<Type, bool>>();

    public void AddRule(EndpointAddress address, Func<Type, bool> rule)
    {
      using (RWLock.AsWriter(_lock))
      {
        _rules[address] = rule;
      }
    }

    public bool ApplyRules(EndpointAddress address, Type handlerType)
    {
      using (RWLock.AsReader(_lock))
      {
        if (!_rules.ContainsKey(address))
        {
          return true;
        }
        return _rules[address](handlerType);
      }
    }
  }
}
