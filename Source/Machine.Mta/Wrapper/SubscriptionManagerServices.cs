using Machine.Container;
using Machine.Container.Plugins;

using MassTransit.ServiceBus.Subscriptions.ServerHandlers;

namespace Machine.Mta.Wrapper
{
  public class SubscriptionManagerServices : IServiceCollection
  {
    #region IServiceCollection Members
    public void RegisterServices(ContainerRegisterer register)
    {
      register.Type<AddSubscriptionHandler>();
      register.Type<RemoveSubscriptionHandler>();
      register.Type<CancelUpdatesHandler>();
      register.Type<CacheUpdateRequestHandler>();
    }
    #endregion
  }
}