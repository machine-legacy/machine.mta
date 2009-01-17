using Machine.Container;
using Machine.Container.Plugins;
using Machine.Mta.InterfacesAsMessages;
using Machine.Mta.Internal;
using Machine.Mta.Timing;
using Machine.Mta.Sagas;

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
      register.Type<DefaultMessageInterfaceImplementationFactory>();
      register.Type<MessageDefinitionFactory>();
      register.Type<MessageDispatcher>();
      register.Type<MessageBusFactory>();
      register.Type<MessageBusManager>();
      register.Type<TimingManager>();
      register.Type<TimingTaskFactory>();
      register.Type<PublishScheduledMessagesTask>();
      register.Type<ScheduleFutureMessages>();
      register.Type<SchedulePublishHandler>();
      register.Type<SagaAspect>();
    }
  }
}
