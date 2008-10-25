using MassTransit.ServiceBus;

namespace Machine.Mta
{
  public interface IServiceBusHub : IHostedService
  {
    IServiceBus Bus
    {
      get;
    }
  }
}