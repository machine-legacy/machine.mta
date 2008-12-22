using System;
using System.Collections.Generic;
using System.Threading;
using Machine.Container.Services;
using Machine.Core.Utility;

namespace Machine.Mta.Internal
{
  public interface IEndpointFactory
  {
    IEndpoint CreateEndpoint(EndpointName name);
  }
  public interface IEndpointResolver
  {
    IEndpoint Resolve(EndpointName name);
  }
  public class EndpointResolver : IEndpointResolver
  {
    readonly Dictionary<EndpointName, IEndpoint> _cache = new Dictionary<EndpointName, IEndpoint>();
    readonly ReaderWriterLock _lock = new ReaderWriterLock();
    readonly IMachineContainer _container;

    public EndpointResolver(IMachineContainer container)
    {
      _container = container;
    }

    public IEndpoint Resolve(EndpointName name)
    {
      using (RWLock.AsReader(_lock))
      {
        if (_cache.ContainsKey(name))
        {
          return _cache[name];
        }
        foreach (IEndpointFactory factory in _container.Resolve.All<IEndpointFactory>())
        {
          IEndpoint resolved = factory.CreateEndpoint(name);
          if (resolved != null)
          {
            _lock.UpgradeToWriterLock(Timeout.Infinite);
            _cache[name] = resolved;
            return resolved;
          }
        }
      }
      throw new InvalidOperationException();
    }
  }
}
