using System;
using System.Collections.Generic;

namespace Machine.Mta.Minimalistic
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
    readonly Guid _sagaId;

    public CurrentSagaContext(Guid sagaId)
    {
      _sagaId = sagaId;
    }

    public static CurrentSagaContext Open(Guid sagaId)
    {
      return _current = new CurrentSagaContext(sagaId);
    }

    public static Guid CurrentSagaId
    {
      get
      {
        if (_current == null)
        {
          TransportMessage transportMessage = CurrentMessageContext.Current;
          if (transportMessage != null)
          {
            return transportMessage.SagaId;
          }
          return Guid.Empty;
        }
        return _current._sagaId;
      }
    }

    public void Dispose()
    {
      _current = null;
    }
  }
}