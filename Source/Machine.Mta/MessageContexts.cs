using System;
using System.Collections.Generic;

namespace Machine.Mta
{
  public class CurrentMessageContext : IDisposable
  {
    [ThreadStatic]
    static CurrentMessageContext _current;
    readonly TransportMessage _transportMessage;

    public CurrentMessageContext(TransportMessage transportMessage)
    {
      _transportMessage = transportMessage;
    }

    public static CurrentMessageContext Open(TransportMessage transportMessage)
    {
      return _current = new CurrentMessageContext(transportMessage);
    }

    public static TransportMessage Current
    {
      get
      {
        if (_current == null)
        {
          return null;
        }
        return _current._transportMessage;
      }
    }

    public void Dispose()
    {
      _current = null;
    }
  }
  public class CurrentCorrelationContext : IDisposable
  {
    [ThreadStatic]
    static CurrentCorrelationContext _current;
    readonly Guid _correlationId;

    public CurrentCorrelationContext(Guid correlationId)
    {
      _correlationId = correlationId;
    }

    public static CurrentCorrelationContext Open(TransportMessage transportMessage)
    {
      return _current = new CurrentCorrelationContext(transportMessage.Id);
    }

    public static Guid CurrentCorrelation
    {
      get
      {
        if (_current == null)
        {
          return Guid.Empty;
        }
        return _current._correlationId;
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

    public CurrentSagaContext(Guid[] sagaIds)
    {
      _sagaIds = sagaIds;
    }

    public static CurrentSagaContext Open(params Guid[] sagaIds)
    {
      return _current = new CurrentSagaContext(sagaIds);
    }

    public static Guid[] CurrentSagaIds
    {
      get
      {
        if (_current == null)
        {
          TransportMessage transportMessage = CurrentMessageContext.Current;
          if (transportMessage != null)
          {
            return transportMessage.SagaIds;
          }
          return new Guid[0];
        }
        return _current._sagaIds;
      }
    }

    public void Dispose()
    {
      _current = null;
    }
  }
}