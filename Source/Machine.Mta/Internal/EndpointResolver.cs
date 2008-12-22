using System;
using System.Collections.Generic;

namespace Machine.Mta.Internal
{
  public interface IEndpointResolver
  {
    IEndpoint Resolve(EndpointName name);
  }
  public class EndpointResolver : IEndpointResolver
  {
    public IEndpoint Resolve(EndpointName name)
    {
      throw new System.NotImplementedException();
    }
  }
}
