using System;
using System.Collections.Generic;
using System.Threading;

using Machine.Container.Services;
using Machine.Core.Utility;

namespace Machine.Mta.Endpoints
{
  public class EndpointResolver : IEndpointResolver
  {
    static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(EndpointResolver));
    readonly Dictionary<EndpointAddress, IEndpoint> _cache = new Dictionary<EndpointAddress, IEndpoint>();
    readonly ReaderWriterLock _lock = new ReaderWriterLock();
    readonly IMachineContainer _container;

    public EndpointResolver(IMachineContainer container)
    {
      _container = container;
    }

    public IEndpoint Resolve(EndpointAddress address)
    {
      using (RWLock.AsReader(_lock))
      {
        if (_cache.ContainsKey(address))
        {
          return _cache[address];
        }
        foreach (IEndpointFactory factory in _container.Resolve.All<IEndpointFactory>())
        {
          IEndpoint resolved = factory.CreateEndpoint(address);
          if (resolved != null)
          {
            _lock.UpgradeToWriterLock(Timeout.Infinite);
            _cache[address] = resolved;
            return resolved;
          }
          else
          {
            _log.Info(factory + " failed to produce.");
          }
        }
      }
      throw new InvalidOperationException("Unable to resolve: " + address);
    }
  }
}