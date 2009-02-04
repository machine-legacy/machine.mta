using System;
using System.Collections.Generic;
using System.Reflection;

namespace Machine.Mta.Helpers
{
  public static class MessageSetupHelpers
  {
    public static void AddMessageTypes(this IMessageRegisterer registerer, params Assembly[] assemblies)
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
      registerer.AddMessageTypes(types);
    }
  }
}
