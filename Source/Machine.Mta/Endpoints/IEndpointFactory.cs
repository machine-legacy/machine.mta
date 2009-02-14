using System;
using System.Collections.Generic;

namespace Machine.Mta.Endpoints
{
  public interface IEndpointFactory
  {
    IEndpoint CreateEndpoint(EndpointAddress address);
  }
}