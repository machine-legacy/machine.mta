using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

using Machine.Core.Utility;

namespace Machine.Mta
{
  public class MessageEndpointLookup : IMessageEndpointLookup 
  {
    private readonly Dictionary<Type, List<EndpointName>> _map = new Dictionary<Type, List<EndpointName>>();
    private readonly List<EndpointName> _catchAlls = new List<EndpointName>();
    private readonly ReaderWriterLock _lock = new ReaderWriterLock();

    public ICollection<EndpointName> LookupEndpointsFor(Type messageType)
    {
      using (RWLock.AsReader(_lock))
      {
        List<EndpointName> destinations = new List<EndpointName>(_catchAlls);
        foreach (KeyValuePair<Type, List<EndpointName>> pair in _map)
        {
          if (pair.Key.IsAssignableFrom(messageType))
          {
            foreach (EndpointName endpointName in pair.Value)
            {
              if (!destinations.Contains(endpointName))
              {
                destinations.Add(endpointName);
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

    public void SendMessageTypeTo(Type messageType, EndpointName destination)
    {
      using (RWLock.AsWriter(_lock))
      {
        if (!_map.ContainsKey(messageType))
        {
          _map[messageType] = new List<EndpointName>();
        }
        if (!_map[messageType].Contains(destination))
        {
          _map[messageType].Add(destination);
        }
      }
    }

    public void SendMessageTypeTo<T>(EndpointName destination)
    {
      SendMessageTypeTo(typeof(T), destination);
    }

    public void SendAllFromAssemblyTo<T>(Assembly assembly, EndpointName destination)
    {
      foreach (Type type in assembly.GetTypes())
      {
        if (typeof(T).IsAssignableFrom(type))
        {
          SendMessageTypeTo(type, destination);
        }
      }
    }
    
    public void SendAllTo(EndpointName destination)
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