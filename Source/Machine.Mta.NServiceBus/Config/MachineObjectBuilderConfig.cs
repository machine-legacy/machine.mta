using System;

using Machine.Container;

using NServiceBus;
using NServiceBus.ObjectBuilder;
using NServiceBus.ObjectBuilder.Common.Config;

namespace Machine.Mta.Config
{
  public static class MachineObjectBuilderConfig
  {
    public static Configure MachineBuilder(this Configure config, IMachineContainer container)
    {
      ConfigureCommon.With(config, new MachineObjectBuilder(container));
      return config;
    }
  }
}
