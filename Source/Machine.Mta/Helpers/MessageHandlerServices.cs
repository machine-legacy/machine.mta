using System;
using System.Reflection;

using Machine.Container;
using Machine.Container.Plugins;
using Machine.Mta.Internal;
using Machine.Mta.Sagas;

namespace Machine.Mta.Helpers
{
  public class MessageHandlerServices : IServiceCollection
  {
    readonly Assembly[] _assemblies;

    public MessageHandlerServices(params Assembly[] assemblies)
    {
      _assemblies = assemblies;
    }

    public void RegisterServices(ContainerRegisterer register)
    {
      foreach (Assembly assembly in _assemblies)
      {
        foreach (Type type in assembly.GetTypes())
        {
          if (typeof(IConsume<IMessage>).IsGenericlyCompatible(type))
          {
            if (typeof(ISagaHandler).IsAssignableFrom(type))
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