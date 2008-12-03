using System;
using System.Collections.Generic;
using System.Reflection;

using Machine.Mta.InterfacesAsMessages;

namespace Machine.Mta.Helpers
{
  public static class MessageSetupHelpers
  {
    public static void RegisterMessageTypes(this MessageInterfaceImplementations implementations, params Assembly[] assemblies)
    {
      List<Type> types = new List<Type>();
      foreach (Assembly assembly in assemblies)
      {
        foreach (Type type in assembly.GetTypes())
        {
          if (typeof(IMessage).IsAssignableFrom(type))
          {
            types.Add(type);
          }
        }
      }
      implementations.GenerateImplementationsOf(types);
    }
  }
}
