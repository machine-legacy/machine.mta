using MassTransit.ServiceBus.Internal;

using Machine.Container;
using Machine.Container.Plugins;
using Machine.Mta.InterfacesAsMessages;
using Machine.Mta.Minimalistic;
using Machine.Mta.Timeouts;

namespace Machine.Mta
{
  public class MtaServices : IServiceCollection
  {
    public virtual void RegisterServices(ContainerRegisterer register)
    {
      register.Type<EndpointResolver>();
      register.Type<MessageEndpointLookup>();
      register.Type<MessageInterfaceTransportFormatter>();
      register.Type<TransportMessageBodySerializer>();
      register.Type<MessageInterfaceImplementations>();
      register.Type<MessageFactory>();
      register.Type<MessageDispatcher>();
      register.Type<MessageBusFactory>();
      register.Type<MessageBusManager>();
      register.Type<TimeoutService>();
      register.Type<TimeoutHandlers>();
    }
  }
}