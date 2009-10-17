using System;
using System.Collections.Generic;

namespace Machine.Mta
{
  public static class CurrentSagaIds
  {
    [ThreadStatic]
    public static Guid[] _incomingIds;
    [ThreadStatic]
    public static Guid[] _outgoingIds;

    public static Guid[] IncomingIds
    {
      get { return _incomingIds; }
    }

    public static Guid[] OutgoingIds
    {
      get { return _outgoingIds; }
    }

    public static void UseIncoming(params Guid[] sagaIds)
    {
      _incomingIds = sagaIds;
    }

    public static void UseOutgoing(params Guid[] sagaIds)
    {
      _outgoingIds = sagaIds;
    }

    public static void CopyIncomingToOutgoing()
    {
      _outgoingIds = _incomingIds;
    }

    public static void Reset()
    {
      _incomingIds = new Guid[0];
      _outgoingIds = new Guid[0];
    }
  }
}
