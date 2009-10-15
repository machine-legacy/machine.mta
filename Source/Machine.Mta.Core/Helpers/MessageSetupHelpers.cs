using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Machine.Mta.Helpers
{
  public static class MessageSetupHelpers
  {
    public static void AddMessageTypes(this IMessageRegisterer registerer, params Assembly[] assemblies)
    {
      registerer.AddMessageTypes(assemblies.SelectMany(assembly => assembly.GetTypes().Where(type => typeof(NServiceBus.IMessage).IsAssignableFrom(type))).Distinct());
    }
  }
}
