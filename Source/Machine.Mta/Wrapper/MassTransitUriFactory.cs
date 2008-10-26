using System;
using System.Collections.Generic;

namespace Machine.Mta.Wrapper
{
  public class MassTransitUriFactory : IMassTransitUriFactory
  {
    private readonly IMassTransitConfigurationProvider _configurationProvider;
    private static readonly Dictionary<Type, IMassTransitUriFactory> _factories = new Dictionary<Type, IMassTransitUriFactory>();

    static MassTransitUriFactory()
    {
      _factories[typeof(MassTransit.ServiceBus.MSMQ.MsmqEndpoint)] = new MsMqUriFactory();
      _factories[typeof(MassTransit.ServiceBus.NMS.NmsEndpoint)] = new NmsUriFactory();
    }

    public MassTransitUriFactory(IMassTransitConfigurationProvider configurationProvider)
    {
      _configurationProvider = configurationProvider;
    }

    public Uri CreateUri(EndpointName name)
    {
      return _factories[_configurationProvider.Configuration.TransportType].CreateUri(name);
    }

    public Uri CreateUri(string name)
    {
      return _factories[_configurationProvider.Configuration.TransportType].CreateUri(name);
    }

    public Uri CreateUri(string address, string name)
    {
      return _factories[_configurationProvider.Configuration.TransportType].CreateUri(address, name);
    }

    public Uri CreateUri(Uri uri)
    {
      return _factories[_configurationProvider.Configuration.TransportType].CreateUri(uri);
    }
  }
}