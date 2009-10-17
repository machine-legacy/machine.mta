using System;

namespace Machine.Mta
{
  public class CurrentMessageContext : IDisposable
  {
    [ThreadStatic]
    static CurrentMessageContext _current;
    readonly EndpointAddress _returnAddress;
    readonly string _correlationId;
    readonly CurrentMessageContext _parentContext;
    bool _stopDispatching;

    public EndpointAddress ReturnAddress
    {
      get { return _returnAddress; }
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

    public CurrentMessageContext(EndpointAddress returnAddress, string correlationId, CurrentMessageContext parentContext)
    {
      _returnAddress = returnAddress;
      _correlationId = correlationId;
      _parentContext = parentContext;
    }

    public static CurrentMessageContext Open(EndpointAddress returnAddress, string correlationId)
    {
      return _current = new CurrentMessageContext(returnAddress, correlationId, _current);
    }

    public static CurrentMessageContext SendRepliesTo(EndpointAddress returnAddress)
    {
      return Open(returnAddress, _current.CorrelationId);
    }

    public static CurrentMessageContext Open(TransportMessage transportMessage)
    {
      return Open(transportMessage.ReturnAddress, transportMessage.CorrelationId);
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