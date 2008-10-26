using System;

namespace Machine.Mta.Wrapper
{
  public interface IServiceBusHubFactory
  {
    IServiceBusHub CreateServerHub(EndpointName endpointName);
    IServiceBusHub CreateSubscriptionManagerHub();
  }
}