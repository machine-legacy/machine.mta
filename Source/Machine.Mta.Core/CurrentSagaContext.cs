using System;
using System.Collections.Generic;
using System.Linq;

namespace Machine.Mta
{
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
      Setup();
    }

    void Setup()
    {
      CurrentSagaIds.UseOutgoing(_sagaIds);
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

    public static CurrentSagaContext OpenForReply()
    {
      return _current = new CurrentSagaContext(_current, CurrentSagaIds.IncomingIds);
    }

    public static Guid[] CurrentIds(bool propogateCurrentMessageIds)
    {
      if (_current == null)
      {
        if (propogateCurrentMessageIds)
        {
          return CurrentSagaIds.IncomingIds;
        }
        return new Guid[0];
      }
      return _current.SagaIds.ToArray();
    }

    public void Dispose()
    {
      CurrentSagaIds.Reset();
      if (_previous != null)
      {
        _previous.Setup();
      }
      _current = _previous;
    }
  }
}