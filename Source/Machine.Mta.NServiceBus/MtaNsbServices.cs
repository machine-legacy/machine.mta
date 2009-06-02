using System;
using System.Collections.Generic;

using Machine.Container;
using Machine.Container.Plugins;

namespace Machine.Mta
{
  public class MtaNsbServices : IServiceCollection
  {
    public void RegisterServices(ContainerRegisterer register)
    {
      register.Type<NsbMessageBusFactory>();
      register.Type<NServiceBusMessageBus>();
    }
  }
}
