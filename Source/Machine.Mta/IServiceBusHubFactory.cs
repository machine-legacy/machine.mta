using System;

namespace Machine.Mta
{
  public interface IServiceBusHubFactory
  {
    IServiceBusHub CreateServerHub(EndpointName endpointName);
    IServiceBusHub CreateSubscriptionManagerHub();
  }
}