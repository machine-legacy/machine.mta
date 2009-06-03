using System;

namespace Machine.Mta
{
  public class CurrentMessageContext : IDisposable
  {
    [ThreadStatic]
    static CurrentMessageContext _current;
    readonly EndpointAddress _returnAddress;
    readonly string _correlationId;
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

    public string CorrelationId
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

    public CurrentMessageContext(EndpointAddress returnAddress, string correlationId, Guid[] sagaIds, CurrentMessageContext parentContext)
    {
      _returnAddress = returnAddress;
      _sagaIds = sagaIds;
      _correlationId = correlationId;
      _parentContext = parentContext;
    }

    public static CurrentMessageContext Open(EndpointAddress returnAddress, string correlationId, Guid[] sagaIds)
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
}