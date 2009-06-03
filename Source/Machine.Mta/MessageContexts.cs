using System;
using System.Collections.Generic;
using System.Linq;

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

  public class CurrentSagaContext : IDisposable
  {
    [ThreadStatic]
    static CurrentSagaContext _current;
    readonly Guid[] _sagaIds;
    readonly CurrentSagaContext _previous;

    protected CurrentSagaContext(CurrentSagaContext previous, Guid[] sagaIds)
    {
      _previous = previous;
      _sagaIds = sagaIds;
    }

    public IEnumerable<Guid> SagaIds
    {
      get
      {
        if (_previous != null)
        {
          foreach (Guid id in _previous.SagaIds)
          {
            yield return id;
          }
        }
        foreach (Guid id in _sagaIds)
        {
          yield return id;
        }
      }
    }

    public static CurrentSagaContext Open(params Guid[] sagaIds)
    {
      return _current = new CurrentSagaContext(_current, sagaIds);
    }

    public static Guid[] CurrentSagaIds(bool propogateCurrentMessageIds)
    {
      if (_current == null)
      {
        if (CurrentMessageContext.Current != null && propogateCurrentMessageIds)
        {
          return CurrentMessageContext.Current.SagaIds;
        }
        return new Guid[0];
      }
      return _current.SagaIds.ToArray();
    }

    public void Dispose()
    {
      _current = _previous;
    }
  }
}