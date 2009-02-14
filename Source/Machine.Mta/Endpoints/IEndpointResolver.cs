using System;
using System.Collections.Generic;

namespace Machine.Mta.Endpoints
{
  public interface IEndpointResolver
  {
    IEndpoint Resolve(EndpointAddress address);
  }
}