using System;
using System.Collections.Generic;
using System.Linq;

namespace Machine.Mta
{
  public class NsbMessageRegisterer : IMessageRegisterer
  {
    readonly List<Type> _messageTypes = new List<Type>();

    public void AddMessageTypes(params Type[] types)
    {
      _messageTypes.AddRange(types);
    }

    public void AddMessageTypes(IEnumerable<Type> types)
    {
      _messageTypes.AddRange(types);
    }

    public IEnumerable<Type> MessageTypes
    {
      get { return _messageTypes.Distinct(); }
    }
  }
}
