using System;
using System.Collections.Generic;

using Machine.Mta.Minimalistic;

namespace Machine.Mta.Wrapper
{
  public class MassTransitUriFactory : IMtaUriFactory
  {
    private readonly IMassTransitConfigurationProvider _configurationProvider;
    private static readonly Dictionary<Type, IMtaUriFactory> _factories = new Dictionary<Type, IMtaUriFactory>();

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