using System;
using System.Reflection;

using MassTransit;

using Machine.Container;
using Machine.Container.Plugins;
using Machine.Mta.Internal;

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
          if (typeof(Consumes<IMessage>.All).IsSortOfContravariantWith(type))
          {
            register.Type(type);
          }
        }
      }
    }
  }
}