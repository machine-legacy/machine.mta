using System;
using System.Collections.Generic;

namespace Machine.Mta
{
  public interface IMessageDestinations : IMessageRouting
  {
    ICollection<EndpointAddress> LookupEndpointsFor(Type messageType, bool throwOnNone);
    void SendMessageTypeTo(Type messageType, params EndpointAddress[] destinations);
    void SendMessageTypeTo<T>(params EndpointAddress[] destinations);
    void SendAllTo(params EndpointAddress[] destination);
  }

  public interface IMessageRouting
  {
    void SubscribeTo<T>(params EndpointAddress[] addresses);
    void AssignOwner<T>(EndpointAddress address);
    ICollection<EndpointAddress> Subscribers(Type messageType);
    EndpointAddress Owner(Type messageType);
  }
}