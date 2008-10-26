using MassTransit.ServiceBus;

namespace Machine.Mta.Wrapper
{
  public interface IServiceBusHub : IHostedService
  {
    IServiceBus Bus
    {
      get;
    }
  }
}