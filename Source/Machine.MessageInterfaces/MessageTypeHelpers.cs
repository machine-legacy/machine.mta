using System;
using System.Collections.Generic;
using System.Reflection;

namespace Machine.Mta.MessageInterfaces
{
  public static class MessageTypeHelpers
  {
    public static IEnumerable<Type> TypesToGenerateForType(Type type)
    {
      foreach (var interfaceType in type.FindInterfaces((ignored, data) => true, null))
      {
        yield return interfaceType;
      }
      yield return type;
    }

    public static IDictionary<string, object> ToDictionary(this object source)
    {
      var dictionary = new Dictionary<string, object>();
      foreach (var property in source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
      {
        dictionary[property.Name] = property.GetValue(source, null);
      }
      return dictionary;
    }
  }
}