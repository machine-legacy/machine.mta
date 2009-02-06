using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

using Machine.Core.Utility;

namespace Machine.Mta
{
  public class MessageDestinations : IMessageDestinations 
  {
    private readonly Dictionary<Type, List<EndpointAddress>> _map = new Dictionary<Type, List<EndpointAddress>>();
    private readonly List<EndpointAddress> _catchAlls = new List<EndpointAddress>();
    private readonly ReaderWriterLock _lock = new ReaderWriterLock();

    public ICollection<EndpointAddress> LookupEndpointsFor(Type messageType)
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
        if (destinations.Count == 0)
        {
          throw new InvalidOperationException("No endpoints for: " + messageType);
        }
        return destinations;
      }
    }

    public void SendMessageTypeTo(Type messageType, EndpointAddress destination)
    {
      using (RWLock.AsWriter(_lock))
      {
        if (!_map.ContainsKey(messageType))
        {
          _map[messageType] = new List<EndpointAddress>();
        }
        if (!_map[messageType].Contains(destination))
        {
          _map[messageType].Add(destination);
        }
      }
    }

    public void SendMessageTypeTo<T>(EndpointAddress destination)
    {
      SendMessageTypeTo(typeof(T), destination);
    }

    public void SendAllFromAssemblyTo<T>(Assembly assembly, EndpointAddress destination)
    {
      foreach (Type type in assembly.GetTypes())
      {
        if (typeof(T).IsAssignableFrom(type))
        {
          SendMessageTypeTo(type, destination);
        }
      }
    }
    
    public void SendAllTo(EndpointAddress destination)
    {
      using (RWLock.AsWriter(_lock))
      {
        if (!_catchAlls.Contains(destination))
        {
          _catchAlls.Add(destination);
        }
      }
    }
  }
}