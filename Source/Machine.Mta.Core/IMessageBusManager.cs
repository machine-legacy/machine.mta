using System;
using System.Collections.Generic;

namespace Machine.Mta
{
  public interface IMessageBusManager
  {
    IMessageBus DefaultBus
    {
      get;
    }
    IMessageBus AddMessageBus(BusProperties properties);
    void EachBus(Action<IMessageBus> action);
  }

  public class BusProperties
  {
    public EndpointAddress ListenAddress { get; set; }
    public EndpointAddress PoisonAddress { get; set; }
    public IEnumerable<Type> AdditionalTypes { get; set; }
    public Int32 NumberOfWorkerThreads { get; set; }
    public TransportType TransportType { get; set; }
    public Func<IEnumerable<Type>, IEnumerable<Type>> OrderHandlers { get; set; }

    public BusProperties()
    {
      this.ListenAddress = EndpointAddress.Null;
      this.PoisonAddress = EndpointAddress.Null;
      this.AdditionalTypes = new Type[0];
      this.NumberOfWorkerThreads = 1;
      this.TransportType = TransportType.Msmq;
      this.OrderHandlers = x => x;
    }
  }

  public enum TransportType
  {
    Msmq,
    RabbitMq
  }
}
