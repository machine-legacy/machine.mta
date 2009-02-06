using System;
using System.Collections.Generic;

namespace Machine.Mta
{
  public static class TypeHelpers
  {
    public static IEnumerable<Type> Interfaces(this Type type)
    {
      if (type.IsInterface)
      {
        yield return type;
      }
      foreach (Type interfaceType in type.GetInterfaces())
      {
        yield return interfaceType;
      }
    }

    public static bool IsImplementationOfGenericType(this Type type, Type genericType)
    {
      Type genericTypeDefinition = genericType;
      if (!genericType.IsGenericTypeDefinition)
      {
        genericTypeDefinition = genericType.GetGenericTypeDefinition();
      }
      IEnumerable<Type> variations = type.AllGenericVariations(genericTypeDefinition);
      if (!genericType.IsGenericTypeDefinition)
      {
        variations = variations.BiggerThan(genericType);
      }
      foreach (Type variation in variations)
      {
        return true;
      }
      return false;
    }

    public static IEnumerable<Type> AllGenericVariations(this Type type, Type genericType)
    {
      if (!genericType.IsGenericTypeDefinition) throw new ArgumentException("genericType");
      if (type.IsGenericType)
      {
        if (type.GetGenericTypeDefinition() == genericType)
        {
          yield return type;
        }
      }
      foreach (Type interfaceType in type.Interfaces())
      {
        if (interfaceType != type)
        {
          foreach (Type yieldMe in interfaceType.AllGenericVariations(genericType))
          {
            yield return yieldMe;
          }
        }
      }
      if (type.BaseType != null)
      {
        foreach (Type yieldMe in type.BaseType.AllGenericVariations(genericType))
        {
          yield return yieldMe;
        }
      }
    }

    public static IEnumerable<Type> BiggerThan(this IEnumerable<Type> types, Type target)
    {
      foreach (Type type in types)
      {
        if (type.GetGenericTypeDefinition() != target.GetGenericTypeDefinition())
        {
          throw new InvalidOperationException("Can only compare the same generic types: " + type + " vs " + target);
        }
        bool compatiable = true;
        for (short i = 0; i < type.GetGenericArguments().Length; ++i)
        {
          Type arg1 = type.GetGenericArguments()[i];
          Type arg2 = target.GetGenericArguments()[i];
          if (!arg1.IsAssignableFrom(arg2))
          {
            compatiable = false;
          }
        }
        if (compatiable)
        {
          yield return type;
        }
      }
    }

    private static Int32 CompareTypes(Type type1, Type type2)
    {
      if (type1 == type2)
      {
        return 0;
      }
      if (type1.IsAssignableFrom(type2))
      {
        return 1;
      }
      if (type2.IsAssignableFrom(type1))
      {
        return -1;
      }
      throw new InvalidOperationException("Types " + type1 + " " + type2 + " are uncomaprable");
    }

    private static Int32 CompareGenericParameterTypes(Type type1, Type type2)
    {
      if (type1.GetGenericTypeDefinition() != type2.GetGenericTypeDefinition())
      {
        throw new InvalidOperationException("Can only compare the same generic types: " + type1 + " vs " + type2);
      }
      for (short i = 0; i < type2.GetGenericArguments().Length; ++i)
      {
        Int32 comparison = CompareTypes(type1.GetGenericArguments()[i], type2.GetGenericArguments()[i]);
        if (comparison != 0)
        {
          return comparison;
        }
      }
      return 0;
    }

    public static Type SmallerType(this IEnumerable<Type> types)
    {
      Type selected = null;
      foreach (Type type in types)
      {
        if (selected == null || CompareGenericParameterTypes(type, selected) < 0)
        {
          selected = type;
        }
      }
      return selected;
    }

    public static bool IsGenericlyCompatible(this Type type1, Type type2)
    {
      if (!type1.IsGenericType)
      {
        foreach (Type type in type1.Interfaces())
        {
          if (type.IsGenericType)
          {
            if (type.IsGenericlyCompatible(type2))
            {
              return true;
            }
          }
        }
        return false;
      }
      foreach (Type interfaceType in type2.Interfaces())
      {
        if (!interfaceType.IsGenericType) continue;
        if (type1.GetGenericTypeDefinition() != interfaceType.GetGenericTypeDefinition())
        {
          continue;
        }
        for (short i = 0; i < type1.GetGenericArguments().Length; ++i)
        {
          Type arg1 = type1.GetGenericArguments()[i];
          Type arg2 = interfaceType.GetGenericArguments()[i];
          if (!arg1.IsAssignableFrom(arg2))
          {
            return false;
          }
        }
        Type genericType = type1.GetGenericTypeDefinition();
        return genericType.MakeGenericType(interfaceType.GetGenericArguments()).IsAssignableFrom(interfaceType);
      }
      return false;
    }
  }
}