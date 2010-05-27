using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

using Machine.Core.Utility;
using Machine.Mta.Configuration;

namespace Machine.Mta
{
  public class MessageRouting : IMessageRouting
  {
    readonly Dictionary<Type, List<EndpointAddress>> _subscriptions = new Dictionary<Type, List<EndpointAddress>>();
    readonly Dictionary<Type, EndpointAddress> _owners = new Dictionary<Type, EndpointAddress>();
    readonly ReaderWriterLock _lock = new ReaderWriterLock();

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
      if (messageType == null)
        return new EndpointAddress[0];
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
        foreach (var owner in _owners)
        {
          if (owner.Key.IsAssignableFrom(messageType))
          {
            return owner.Value;
          }
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

  public class MessageRoutingWithConfiguration : IMessageRoutingWithConfiguration
  {
    readonly IMessageRouting _routing;
    readonly MessageBusConfigurationSection _configuration;

    public MessageRoutingWithConfiguration(IMessageRouting routing)
    {
      _routing = routing;
      _configuration = MessageBusConfigurationSection.Read();
    }

    public void SubscribeTo<T>(params string[] addresses)
    {
      _routing.SubscribeTo<T>(addresses.Select(address => _configuration.Lookup(address).Single()).ToArray());
    }

    public void AssignOwner<T>(string address)
    {
      _routing.AssignOwner<T>(_configuration.Lookup(address).Single());
    }
  }
}