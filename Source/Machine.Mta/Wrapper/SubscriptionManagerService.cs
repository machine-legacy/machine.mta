using MassTransit.ServiceBus;
using MassTransit.ServiceBus.Subscriptions;

namespace Machine.Mta.Wrapper
{
  public class SubscriptionManagerService : AbstractSubscriptionService
  {
    public SubscriptionManagerService(IServiceBus bus, ISubscriptionCache subscriptionCache, ISubscriptionRepository subscriptionRepository)
      : base(new SubscriptionService(bus, subscriptionCache, subscriptionRepository))
    {
    }
  }
}