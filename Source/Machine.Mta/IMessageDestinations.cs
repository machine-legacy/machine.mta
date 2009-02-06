using System;
using System.Collections.Generic;
using System.Reflection;

namespace Machine.Mta
{
  public interface IMessageDestinations
  {
    ICollection<EndpointAddress> LookupEndpointsFor(Type messageType);
    void SendMessageTypeTo(Type messageType, EndpointAddress destination);
    void SendMessageTypeTo<T>(EndpointAddress destination);
    void SendAllFromAssemblyTo<T>(Assembly assembly, EndpointAddress destination);
    void SendAllTo(EndpointAddress destination);
  }
}