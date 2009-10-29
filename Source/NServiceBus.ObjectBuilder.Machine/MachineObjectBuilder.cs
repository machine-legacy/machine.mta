using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Machine.Container;
using Machine.Core.Utility;

using NServiceBus.ObjectBuilder.Common;

namespace NServiceBus.ObjectBuilder.Machine
{
  public class MachineObjectBuilder : IContainer
  {
    readonly IMachineContainer _container;
    readonly Dictionary<Type, Resolver> _resolvers = new Dictionary<Type, Resolver>();

    public MachineObjectBuilder(IMachineContainer container)
    {
      _container = container;
    }

    public object Build(Type typeToBuild)
    {
      return ConfigureIfCapabale(_container.Resolve.Object(typeToBuild));
    }

    public IEnumerable<object> BuildAll(Type typeToBuild)
    {
      var instances = _container.Resolve.All(typeToBuild);
      return instances.Select(i => ConfigureIfCapabale(i));
    }

    object ConfigureIfCapabale(object instance)
    {
      var type = instance.GetType();
      if (_resolvers.ContainsKey(type))
      {
        return _resolvers[type].ConfigureIfNecessary(instance);
      }
      return instance;
    }

    public void Configure(Type type, ComponentCallModelEnum callModel)
    {
      switch (callModel)
      {
        case ComponentCallModelEnum.Singleton:
          _resolvers[type] = new SingletonResolver(type, _container, Build);
          _container.Register.Type(type).AsSingleton();
          break;
        case ComponentCallModelEnum.Singlecall:
          _resolvers[type] = new TransientResolver(type, _container, Build);
          _container.Register.Type(type).AsTransient();
          break;
        default:
          throw new ArgumentException();
      }
    }

    public void ConfigureProperty(Type type, string property, object value)
    {
      if (!_resolvers.ContainsKey(type))
        throw new ArgumentException("Type was NOT registered via the ObjectBuilder: " + type);
      _resolvers[type].ConfigureProperty(property, value);
    }

    public void RegisterSingleton(Type lookupType, object instance)
    {
      _resolvers[lookupType] = new SingletonResolver(lookupType, _container, Build);
      _container.Register.Type(lookupType).Is(instance);
    }
  }

  public abstract class Resolver
  {
    readonly Type _type;
    readonly IMachineContainer _container;
    readonly Func<Type, object> _buildAndConfigure;
    readonly Dictionary<string, object> _configuration = new Dictionary<string, object>();

    protected Resolver(Type type, IMachineContainer container, Func<Type, object> buildAndConfigure)
    {
      _type = type;
      _buildAndConfigure = buildAndConfigure;
      _container = container;
    }

    public virtual object ConfigureIfNecessary(object instance)
    {
      var type = instance.GetType();
      foreach (var propertyInfo in type.GetProperties())
      {
        if (propertyInfo.CanWrite)
        {
          if (_container.CanResolve(propertyInfo.PropertyType))
          {
            propertyInfo.SetValue(instance, _buildAndConfigure(propertyInfo.PropertyType), new object[0]);
          }
          if (_configuration.ContainsKey(propertyInfo.Name))
          {
            propertyInfo.SetValue(instance, _configuration[propertyInfo.Name], new object[0]);
          }
        }
      }
      return instance;
    }

    public void ConfigureProperty(string property, object value)
    {
      _configuration[property] = value;
    }
  }

  public class SingletonResolver : TransientResolver
  {
    readonly List<object> _instances = new List<object>();
    readonly ReaderWriterLock _lock = new ReaderWriterLock();

    public SingletonResolver(Type type, IMachineContainer container, Func<Type, object> buildAndConfigure)
      : base(type, container, buildAndConfigure)
    {
    }

    public override object ConfigureIfNecessary(object instance)
    {
      using (RWLock.AsReader(_lock))
      {
        if (RWLock.UpgradeToWriterIf(_lock, () => !_instances.Contains(instance)))
        {
          base.ConfigureIfNecessary(instance);
          _instances.Add(instance);
        }
        return instance;
      }
    }
  }

  public class TransientResolver : Resolver
  {
    public TransientResolver(Type type, IMachineContainer container, Func<Type, object> buildAndConfigure)
      : base(type, container, buildAndConfigure)
    {
    }
  }
}