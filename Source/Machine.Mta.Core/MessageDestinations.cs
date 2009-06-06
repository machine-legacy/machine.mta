using System;
using System.Collections.Generic;
using System.Threading;

using Machine.Core.Utility;
using System.Linq;

namespace Machine.Mta
{
  public class MessageDestinations : IMessageDestinations 
  {
    readonly Dictionary<Type, List<EndpointAddress>> _map = new Dictionary<Type, List<EndpointAddress>>();
    readonly Dictionary<Type, List<EndpointAddress>> _subscriptions = new Dictionary<Type, List<EndpointAddress>>();
    readonly Dictionary<Type, EndpointAddress> _owners = new Dictionary<Type, EndpointAddress>();
    readonly List<EndpointAddress> _catchAlls = new List<EndpointAddress>();
    readonly ReaderWriterLock _lock = new ReaderWriterLock();

    public ICollection<EndpointAddress> LookupEndpointsFor(Type messageType, bool throwOnNone)
    {
      using (RWLock.AsReader(_lock))
      {
        List<EndpointAddress> destinations = new List<EndpointAddress>(_catchAlls);
        foreach (KeyValuePair<Type, List<EndpointAddress>> pair in _map)
        {
          if (pair.Key.IsAssignableFrom(messageType))
          {
            foreach (EndpointAddress endpointAddress in pair.Value)
            {
              if (!destinations.Contains(endpointAddress))
              {
                destinations.Add(endpointAddress);
              }
            }
          }
        }
        if (destinations.Count == 0 && throwOnNone)
        {
          throw new InvalidOperationException("No endpoints for: " + messageType);
        }
        return destinations;
      }
    }

    public void SendMessageTypeTo(Type messageType, params EndpointAddress[] destinations)
    {
      using (RWLock.AsWriter(_lock))
      {
        if (!_map.ContainsKey(messageType))
        {
          _map[messageType] = new List<EndpointAddress>();
        }
        foreach (EndpointAddress destination in destinations)
        {
          if (!_map[messageType].Contains(destination))
          {
            _map[messageType].Add(destination);
          }
        }
      }
    }

    public void SendMessageTypeTo<T>(params EndpointAddress[] destinations)
    {
      SendMessageTypeTo(typeof(T), destinations);
    }
    
    public void SendAllTo(params EndpointAddress[] destinations)
    {
      using (RWLock.AsWriter(_lock))
      {
        foreach (EndpointAddress destination in destinations)
        {
          if (!_catchAlls.Contains(destination))
          {
            _catchAlls.Add(destination);
          }
        }
      }
    }

    public void SubscribeTo<T>(params EndpointAddress[] addresses)
    {
      using (RWLock.AsWriter(_lock))
      {
        if (!_subscriptions.ContainsKey(typeof(T)))
        {
          _subscriptions[typeof(T)] = new List<EndpointAddress>();
        }
        foreach (var address in addresses)
        {
          if (!_subscriptions[typeof(T)].Contains(address))
          {
            _subscriptions[typeof(T)].Add(address);
          }
        }
      }
    }

    public void AssignOwner<T>(EndpointAddress address)
    {
      using (RWLock.AsWriter(_lock))
      {
        if (_owners.ContainsKey(typeof(T)))
        {
          throw new ArgumentException(address + " already owns " + typeof(T));
        }
        _owners[typeof(T)] = address;
      }
    }

    public ICollection<EndpointAddress> Subscribers(Type messageType)
    {
      using (RWLock.AsReader(_lock))
      {
        if (_subscriptions.ContainsKey(messageType))
        {
          return _subscriptions[messageType].ToArray();
        }
        return new EndpointAddress[0];
      }
    }

    public EndpointAddress Owner(Type messageType)
    {
      using (RWLock.AsReader(_lock))
      {
        if (_owners.ContainsKey(messageType))
        {
          return _owners[messageType];
        }
        return null;
      }
    }

    public IEnumerable<Type> MessageTypes()
    {
      using (RWLock.AsReader(_lock))
      {
        return _owners.Keys.Union(_subscriptions.Keys).ToArray();
      }
    }
  }
}