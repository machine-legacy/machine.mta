using System;
using System.Collections.Generic;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.Unicast.Transport.RabbitMQ.Config
{
  public static class ConfigureAmqp
  {
    public static ConfigAmqpTransport AmqpTransport(this Configure config)
    {
      var cfg = new ConfigAmqpTransport();
      cfg.Configure(config);
      return cfg;
    }
  }

  public class ConfigAmqpTransport : Configure
  {
    private IComponentConfig<RabbitMqTransport> _config;

    public void Configure(Configure config)
    {
      this.Builder = config.Builder;
      this.Configurer = config.Configurer;

      _config = Configurer.ConfigureComponent<RabbitMqTransport>(ComponentCallModelEnum.Singleton);
      _config.ConfigureProperty(t => t.NumberOfWorkerThreads, 1);
      _config.ConfigureProperty(t => t.MaximumNumberOfRetries, 3);
      _config.ConfigureProperty(t => t.TransactionTimeout, TimeSpan.FromMinutes(5));
    }

    public ConfigAmqpTransport MaximumNumberOfRetries(Int32 value)
    {
      _config.ConfigureProperty(t => t.MaximumNumberOfRetries, value);
      return this;
    }

    public ConfigAmqpTransport NumberOfWorkerThreads(Int32 value)
    {
      _config.ConfigureProperty(t => t.NumberOfWorkerThreads, value);
      return this;
    }

    public ConfigAmqpTransport TransactionTimeout(TimeSpan value)
    {
      _config.ConfigureProperty(t => t.TransactionTimeout, value);
      return this;
    }

    public ConfigAmqpTransport On(string listenAddress, string poisonAddress)
    {
      if (!String.IsNullOrEmpty(listenAddress))
        _config.ConfigureProperty(t => t.ListenAddress, listenAddress);
      if (!String.IsNullOrEmpty(poisonAddress))
        _config.ConfigureProperty(t => t.PoisonAddress, poisonAddress);
      return this;
    }
  }
}
