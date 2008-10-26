using Machine.Container;
using Machine.Container.Plugins;

using Machine.Mta.InterfacesAsMessages;
using Machine.Mta.LowerLevelMessageBus;

namespace Machine.Mta
{
  public class MtaServices : IServiceCollection
  {
    #region IServiceCollection Members
    public virtual void RegisterServices(ContainerRegisterer register)
    {
      register.Type<MessageEndpointLookup>();
      register.Type<MessageInterfaceTransportFormatter>();
      register.Type<TransportMessageBodySerializer>();
      register.Type<MessageInterfaceImplementations>();
      register.Type<MessageFactory>();
      register.Type<MessageDispatcher>();
      register.Type<MessageBusFactory>();
      register.Type<MessageBusManager>();
    }
    #endregion
  }
}