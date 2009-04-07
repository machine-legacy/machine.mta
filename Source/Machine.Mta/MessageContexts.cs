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
    readonly EndpointAddress _returnAddress;
    readonly Guid _correlationId;
    readonly Guid[] _sagaIds;
    readonly CurrentMessageContext _parentContext;
    bool _stopDispatching;

    public EndpointAddress ReturnAddress
    {
      get { return _returnAddress; }
    }

    public Guid[] SagaIds
    {
      get { return _sagaIds; }
    }

    public Guid CorrelationId
    {
      get { return _correlationId; }
    }

    public bool AskedToStopDispatchingCurrentMessageToHandlers
    {
      get { return _stopDispatching; }
    }

    public void StopDispatchingCurrentMessageToHandlers()
    {
      _stopDispatching = true;
    }

    public CurrentMessageContext(EndpointAddress returnAddress, Guid correlationId, Guid[] sagaIds, CurrentMessageContext parentContext)
    {
      _returnAddress = returnAddress;
      _sagaIds = sagaIds;
      _correlationId = correlationId;
      _parentContext = parentContext;
    }

    public static CurrentMessageContext Open(EndpointAddress returnAddress, Guid correlationId, Guid[] sagaIds)
    {
      return _current = new CurrentMessageContext(returnAddress, correlationId, sagaIds, _current);
    }

    public static CurrentMessageContext SendRepliesTo(EndpointAddress returnAddress)
    {
      return Open(returnAddress, _current.CorrelationId, _current.SagaIds);
    }

    public static CurrentMessageContext Open(TransportMessage transportMessage)
    {
      return Open(transportMessage.ReturnAddress, transportMessage.CorrelationId, transportMessage.SagaIds);
    }

    public static CurrentMessageContext Current
    {
      get { return _current; }
    }

    public void Dispose()
    {
      _current = _parentContext;
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