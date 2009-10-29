using System;

using Machine.Container;

using NServiceBus.ObjectBuilder.Common.Config;

namespace NServiceBus.ObjectBuilder.Machine.Config
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
