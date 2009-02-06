using System;
using System.Reflection;

using Machine.Container;
using Machine.Container.Plugins;
using Machine.Mta.Sagas;

namespace Machine.Mta.Helpers
{
  public class SagaRepositoryServices : IServiceCollection
  {
    readonly Assembly[] _assemblies;

    public SagaRepositoryServices(params Assembly[] assemblies)
    {
      _assemblies = assemblies;
    }

    public void RegisterServices(ContainerRegisterer register)
    {
      foreach (Assembly assembly in _assemblies)
      {
        foreach (Type type in assembly.GetExportedTypes())
        {
          if (type.IsImplementationOfGenericType(typeof(ISagaStateRepository<>)))
          {
            register.Type(type);
          }
        }
      }
    }
  }
}