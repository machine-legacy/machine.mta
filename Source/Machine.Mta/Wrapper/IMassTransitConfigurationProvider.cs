using System;
using System.Collections.Generic;

namespace Machine.Mta.Wrapper
{
  public interface IMassTransitConfigurationProvider
  {
    MassTransitConfiguration Configuration
    {
      get;
    }
  }
}
