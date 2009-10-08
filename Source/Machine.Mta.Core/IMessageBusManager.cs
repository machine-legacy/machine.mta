using System;
using System.Collections.Generic;
using NServiceBus.Unicast.Transport.Msmq;

namespace Machine.Mta
{
  public interface IMessageBusManager
  {
    IMessageBus DefaultBus
    {
      get;
    }

    IMessageBus AddMessageBus(BusProperties properties);
    // IMessageBus AddSendOnlyMessageBus();
    // IMessageBus AddMessageBus(EndpointAddress address, EndpointAddress poisonAddress);
    // IMessageBus AddMessageBus(EndpointAddress address, EndpointAddress poisonAddress, ThreadPoolConfiguration threadPoolConfiguration);
    // IMessageBus AddMessageBus(EndpointAddress address, EndpointAddress poisonAddress, IProvideHandlerTypes handlerTypes);
    // IMessageBus AddMessageBus(EndpointAddress address, EndpointAddress poisonAddress, IProvideHandlerTypes handlerTypes, ThreadPoolConfiguration threadPoolConfiguration);
    // IMessageBus UseSingleBus(EndpointAddress address, EndpointAddress poisonAddress);
    void EachBus(Action<IMessageBus> action);
  }

  public class BusProperties
  {
    public EndpointAddress ListenAddress { get; set; }
    public EndpointAddress PoisonAddress { get; set; }
    public IEnumerable<Type> AdditionalTypes { get; set; }
    public Int32 NumberOfWorkerThreads { get; set; }
    public TransportType TransportType { get; set; }

    public BusProperties()
    {
      this.ListenAddress = EndpointAddress.Null;
      this.PoisonAddress = EndpointAddress.Null;
      this.AdditionalTypes = new Type[0];
      this.NumberOfWorkerThreads = 1;
      this.TransportType = TransportType.Msmq;
    }
  }

  public enum TransportType
  {
    Msmq,
    RabbitMq
  }
}
