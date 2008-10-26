using MassTransit.ServiceBus;
using MassTransit.ServiceBus.Subscriptions;

namespace Machine.Mta.Wrapper
{
  public class ServerSubscriptionService : AbstractSubscriptionService
  {
    public ServerSubscriptionService(IServiceBus bus, ISubscriptionCache subscriptionCache, IEndpoint subscriptionManagerEndpoint)
      : base(new SubscriptionClient(bus, subscriptionCache, subscriptionManagerEndpoint))
    {
    }
  }
}