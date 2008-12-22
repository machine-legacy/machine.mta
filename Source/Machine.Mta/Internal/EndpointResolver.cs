using System;
using System.Collections.Generic;

namespace Machine.Mta.Internal
{
  public interface IEndpointResolver
  {
    IEndpoint Resolve(Uri uri);
  }
  public class EndpointResolver : IEndpointResolver
  {
    public IEndpoint Resolve(Uri uri)
    {
      throw new System.NotImplementedException();
    }
  }
}
