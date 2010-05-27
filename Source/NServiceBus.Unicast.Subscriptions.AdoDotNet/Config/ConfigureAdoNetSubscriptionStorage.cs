using System;
using System.Collections.Generic;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.Unicast.Subscriptions.AdoDotNet.Config
{
  public static class ConfigureAdoNetSubscriptionStorage
  {
    public static Configure AdoNetSubscriptionStorage(this Configure config)
    {
      config.Configurer.ConfigureComponent<AdoNetSubscriptionStorage>(ComponentCallModelEnum.Singleton);
      return config;
    }
  }
}
