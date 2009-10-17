using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace Machine.Mta.MessageInterfaces
{
  public class MessageInterfaceSerializationBinder : SerializationBinder
  {
    public override Type BindToType(string assemblyName, string typeName)
    {
      return MessageInterfaceHelpers.FindTypeNamed(typeName, false);
    }
  }

  public static class MessageInterfaceHelpers
  {
    public static Type FindTypeNamed(string name, bool throwOnNull)
    {
      Type deserializeAs = Type.GetType(name);
      if (deserializeAs != null)
      {
        return deserializeAs;
      }
      foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
        deserializeAs = assembly.GetType(name);
        if (deserializeAs != null && typeof(IMessage).IsAssignableFrom(deserializeAs))
        {
          return deserializeAs;
        }
      }
      if (throwOnNull)
      {
        throw new InvalidOperationException("Unable to find " + name);
      }
      return null;
    }
  }
}