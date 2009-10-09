using System;
using System.Collections.Generic;
using System.Reflection;

namespace Machine.Mta.InterfacesAsMessages
{
  public static class MessageTypeHelpers
  {
    public static IEnumerable<Type> TypesToGenerateForType(Type type)
    {
      foreach (Type interfaceType in type.FindInterfaces((ignored, data) => true, null))
      {
        yield return interfaceType;
      }
      yield return type;
    }

    public static IDictionary<string, object> ToDictionary(this object source)
    {
      Dictionary<string, object> dictionary = new Dictionary<string, object>();
      foreach (PropertyInfo property in source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
      {
        dictionary[property.Name] = property.GetValue(source, null);
      }
      return dictionary;
    }
  }
}