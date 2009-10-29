using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus.MessageInterfaces;
using NServiceBus.Unicast.Subscriptions;

namespace Machine.Mta
{
  public class StaticSubscriptionStorage : ISubscriptionStorage
  {
    readonly static log4net.ILog _log = log4net.LogManager.GetLogger(typeof(StaticSubscriptionStorage));
    readonly IMessageRouting _routing;
    readonly IMessageMapper _mapper;

    public StaticSubscriptionStorage(IMessageRouting routing, IMessageMapper mapper)
    {
      _routing = routing;
      _mapper = mapper;
    }

    public void Subscribe(string client, IList<string> messageTypes)
    {
      _log.Info("Add: " + client + " " + String.Join(", ", messageTypes.ToArray()));
    }

    public void Unsubscribe(string client, IList<string> messageTypes)
    {
      _log.Info("Remove: " + client + " " + String.Join(", ", messageTypes.ToArray()));
    }

    public IList<string> GetSubscribersForMessage(IList<string> messageTypes)
    {
      var found = new List<string>();
      foreach (var messageTypeName in messageTypes)
      {
        var messageType = _mapper.GetMappedTypeFor(messageTypeName);
        foreach (var destiny in _routing.Subscribers(messageType))
        {
          found.Add(destiny.ToString());
        }
      }
      return found;
    }

    public void Init()
    {
      _log.Info("Initialize");
    }
  }
}
