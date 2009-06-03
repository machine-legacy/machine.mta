using System;
using System.Collections.Generic;

using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Transport;

namespace Machine.Mta
{
  public class StaticSubscriptionStorage : ISubscriptionStorage
  {
    readonly static log4net.ILog _log = log4net.LogManager.GetLogger(typeof(StaticSubscriptionStorage));
    readonly IMessageRouting _routing;

    public StaticSubscriptionStorage(IMessageRouting routing)
    {
      _routing = routing;
    }

    public void HandleSubscriptionMessage(TransportMessage msg)
    {
      NServiceBus.IMessage[] messages = msg.Body;
      if ((messages != null) && (messages.Length == 1))
      {
        SubscriptionMessage subMessage = messages[0] as SubscriptionMessage;
        if (subMessage != null)
        {
          if (subMessage.SubscriptionType == SubscriptionType.Add)
          {
            _log.Info("Add: " + subMessage.TypeName + " " + msg.ReturnAddress);
          }
          if (subMessage.SubscriptionType == SubscriptionType.Remove)
          {
            _log.Info("Remove: " + subMessage.TypeName + " " + msg.ReturnAddress);
          }
        }
      }
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
