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

  public class CurrentMessageContext : IDisposable
  {
    [ThreadStatic]
    static CurrentMessageContext _current;
    readonly TransportMessage _transportMessage;
    bool _stopDispatching;

    public TransportMessage TransportMessage
    {
      get { return _transportMessage; }
    }

    public bool AskedToStopDispatchingCurrentMessageToHandlers
    {
      get { return _stopDispatching; }
    }

    public void StopDispatchingCurrentMessageToHandlers()
    {
      _stopDispatching = true;
    }

    public CurrentMessageContext(TransportMessage transportMessage)
    {
      _transportMessage = transportMessage;
    }

    public static CurrentMessageContext Open(TransportMessage transportMessage)
    {
      return _current = new CurrentMessageContext(transportMessage);
    }

    public static TransportMessage CurrentTransportMessage
    {
      get
      {
        if (_current == null)
        {
          return null;
        }
        return _current.TransportMessage;
      }
    }

    public static CurrentMessageContext Current
    {
      get
      {
        if (_current == null)
        {
          return null;
        }
        return _current;
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
        TransportMessage transportMessage = CurrentMessageContext.CurrentTransportMessage;
        if (transportMessage != null && propogateCurrentMessageIds)
        {
          return transportMessage.SagaIds;
        }
        return new Guid[0];
      }
      return _current.SagaIds.ToArray();
    }

    public void Dispose()
    {
      _current = null;
    }
  }
}