using System;
using System.Collections.Generic;

using Machine.Container.Services;

using NServiceBus.ObjectBuilder;
using NServiceBus.ObjectBuilder.Common;

namespace Machine.Mta
{
  public class MachineObjectBuilder : IContainer
  {
    readonly IMachineContainer _container;
    readonly Dictionary<Type, Dictionary<string, object>> _configuration = new Dictionary<Type, Dictionary<string, object>>();

    public MachineObjectBuilder(IMachineContainer container)
    {
      _container = container;
    }

    public object Build(Type typeToBuild)
    {
      var instance = _container.Resolve.Object(typeToBuild);
      var type = instance.GetType();
      var configuration = new Dictionary<string, object>();
      if (_configuration.ContainsKey(type))
      {
        configuration = _configuration[type];
      }
      foreach (var propertyInfo in type.GetProperties())
      {
        if (propertyInfo.CanWrite)
        {
          if (_container.CanResolve(propertyInfo.PropertyType))
          {
            propertyInfo.SetValue(instance, Build(propertyInfo.PropertyType), new object[0]);
          }
          if (configuration.ContainsKey(propertyInfo.Name))
          {
            propertyInfo.SetValue(instance, _configuration[type][propertyInfo.Name], new object[0]);
          }
        }
      }
      return instance;
    }

    public IEnumerable<object> BuildAll(Type typeToBuild)
    {
      return _container.Resolve.All(typeToBuild);
    }

    public void Configure(Type component, ComponentCallModelEnum callModel)
    {
      switch (callModel)
      {
        case ComponentCallModelEnum.Singleton:
          _container.Register.Type(component).AsSingleton();
          break;
        case ComponentCallModelEnum.Singlecall:
          _container.Register.Type(component).AsTransient();
          break;
        default:
          throw new ArgumentException();
      }
    }

    public void ConfigureProperty(Type component, string property, object value)
    {
      if (!_configuration.ContainsKey(component)) _configuration[component] = new Dictionary<string, object>();
      if (!_configuration[component].ContainsKey(property)) _configuration[component][property] = value;
    }

    public void RegisterSingleton(Type lookupType, object instance)
    {
      _container.Register.Type(lookupType).Is(instance);
    }
  }
}