using System;
using System.Collections.Generic;
using System.Reflection;

namespace Machine.Mta
{
  public interface IMessageDestinations
  {
    ICollection<EndpointAddress> LookupEndpointsFor(Type messageType, bool throwOnNone);
    void SendMessageTypeTo(Type messageType, params EndpointAddress[] destinations);
    void SendMessageTypeTo<T>(params EndpointAddress[] destinations);
    void SendAllFromAssemblyTo<T>(Assembly assembly, EndpointAddress destination);
    void SendAllTo(params EndpointAddress[] destination);
  }
}