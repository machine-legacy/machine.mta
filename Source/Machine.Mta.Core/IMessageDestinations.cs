using System;
using System.Collections.Generic;

namespace Machine.Mta
{
  public interface IMessageRouting
  {
    void SubscribeTo<T>(params EndpointAddress[] addresses);
    void AssignOwner<T>(EndpointAddress address);
    ICollection<EndpointAddress> Subscribers(Type messageType);
    EndpointAddress Owner(Type messageType);
    IEnumerable<Type> MessageTypes();
  }

  public interface IMessageRoutingWithConfiguration
  {
    void SubscribeTo<T>(params string[] addresses);
    void AssignOwner<T>(string address);
  }
}