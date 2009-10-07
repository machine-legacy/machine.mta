using System;
using System.Collections.Generic;

namespace Machine.Mta
{
  public class CurrentMessageBus : IDisposable
  {
    [ThreadStatic]
    static CurrentMessageBus _current;
    readonly IMessageBus _bus;

    public CurrentMessageBus(IMessageBus bus)
    {
      _bus = bus;
    }

    public static CurrentMessageBus Open(IMessageBus bus)
    {
      return _current = new CurrentMessageBus(bus);
    }

    public static IMessageBus Current
    {
      get
      {
        if (_current == null)
        {
          return null;
        }
        return _current._bus;
      }
    }

    public void Dispose()
    {
      _current = null;
    }
  }
}
