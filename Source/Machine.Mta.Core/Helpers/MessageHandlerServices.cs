using System;
using System.Reflection;

using Machine.Container;
using Machine.Container.Plugins;

namespace Machine.Mta.Helpers
{
  public class MessageHandlerServices : IServiceCollection
  {
    readonly IInspectBusTypes _inspectBusTypes;
    readonly Assembly[] _assemblies;

    public MessageHandlerServices(IInspectBusTypes inspectBusTypes, params Assembly[] assemblies)
    {
      _inspectBusTypes = inspectBusTypes;
      _assemblies = assemblies;
    }

    public void RegisterServices(ContainerRegisterer register)
    {
      foreach (Assembly assembly in _assemblies)
      {
        foreach (Type type in assembly.GetTypes())
        {
          if (_inspectBusTypes.IsConsumer(type))
          {
            if (_inspectBusTypes.IsSagaConsumer(type))
            {
              register.Type(type).AsTransient();
            }
            else
            {
              register.Type(type);
            }
          }
        }
      }
    }
  }
}