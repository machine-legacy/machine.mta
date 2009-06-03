using System;
using System.Collections.Generic;

using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Transport;

namespace Machine.Mta
{
  public class StaticSubscriptionStorage : ISubscriptionStorage
  {
    readonly IMessageRouting _routing;

    public StaticSubscriptionStorage(IMessageRouting routing)
    {
      _routing = routing;
    }

    public void HandleSubscriptionMessage(TransportMessage msg)
    {
      throw new NotSupportedException();
    }

    public IList<string> GetSubscribersForMessage(Type messageType)
    {
      var found = new List<string>();
      foreach (var destiny in _routing.Subscribers(messageType))
      {
        found.Add(destiny.ToNsbAddress());
      }
      return found;
    }

    public void Init(IList<Type> messageTypes)
    {
    }
  }
}
