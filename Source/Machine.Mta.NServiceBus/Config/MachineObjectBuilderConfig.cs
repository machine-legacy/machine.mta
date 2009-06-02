using System;
using Machine.Container.Services;

using NServiceBus;
using NServiceBus.ObjectBuilder;
using NServiceBus.ObjectBuilder.Common.Config;

namespace Machine.Mta.Config
{
  public static class MachineObjectBuilderConfig
  {
    public static Configure MachineBuilder(this Configure config, IMachineContainer container, params Action<IConfigureComponents>[] configActions)
    {
      ConfigureCommon.With(config, new MachineObjectBuilder(container), configActions);
      return config;
    }
  }
}
