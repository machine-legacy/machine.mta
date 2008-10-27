using System;
using System.Collections.Generic;

namespace Machine.Mta.Minimalistic
{
  public static class InvarianceHelpers
  {
    public static bool IsSortOfContravariantWith(this Type type1, Type type2)
    {
      foreach (Type interfaceType in type1.GetInterfaces())
      {
        if (interfaceType.IsSortOfContravariantWith(type2))
        {
          return true;
        }
      }
      foreach (Type interfaceType in type2.GetInterfaces())
      {
        if (type1.IsSortOfContravariantWith(interfaceType))
        {
          return true;
        }
      }
      if (type1.GetGenericArguments().Length != type2.GetGenericArguments().Length)
      {
        return false;
      }
      if (type1.GetGenericArguments().Length == 0)
      {
        return type1.IsAssignableFrom(type2);
      }
      for (short i = 0; i < type1.GetGenericArguments().Length; ++i)
      {
        Type arg1 = type1.GetGenericArguments()[i];
        Type arg2 = type2.GetGenericArguments()[i];
        if (!arg1.IsAssignableFrom(arg2))
        {
          return false;
        }
      }
      Type genericType = type1.GetGenericTypeDefinition();
      return genericType.MakeGenericType(type2.GetGenericArguments()).IsAssignableFrom(type2);
    }
  }
  
  public class InvarianceHelpersSpecs
  {
    public void should_all_be_true()
    {
      Console.WriteLine(typeof(IReader<IBase>).IsSortOfContravariantWith(typeof(IReader<IDerrived>)));
      Console.WriteLine(!typeof(IReader<IDerrived>).IsSortOfContravariantWith(typeof(IReader<IBase>)));
      Console.WriteLine(!typeof(IReader<IDerrived>).IsSortOfContravariantWith(typeof(IReader<IAnotherDerrived>)));
      Console.WriteLine(typeof(IReader<string>).IsSortOfContravariantWith(typeof(DoesStrings)));
      Console.WriteLine(typeof(IReader<object>).IsSortOfContravariantWith(typeof(DoesStrings)));
      Console.WriteLine(typeof(IWriter<string>).IsSortOfContravariantWith(typeof(DoesStrings)));
      Console.WriteLine(typeof(IWriter<object>).IsSortOfContravariantWith(typeof(DoesStrings)));
      Console.WriteLine(!typeof(IWriter<short>).IsSortOfContravariantWith(typeof(DoesStrings)));
      Console.WriteLine(typeof(IReader<IBase>).IsSortOfContravariantWith(typeof(DoesDerrived)));
      Console.WriteLine(typeof(IReader<IDerrived>).IsSortOfContravariantWith(typeof(DoesDerrived)));
      Console.WriteLine(typeof(IReader<object>).IsSortOfContravariantWith(typeof(DoesDerrived)));
      Console.WriteLine(!typeof(IReader<ISomethingElse>).IsSortOfContravariantWith(typeof(DoesDerrived)));
      Console.WriteLine(typeof(IReader<object>).IsSortOfContravariantWith(typeof(IReader<string>)));
      Console.WriteLine(!typeof(IReader<Int32>).IsSortOfContravariantWith(typeof(IReader<string>)));
    }

    interface IReader<T> { T Read(); }
    interface IWriter<T> { void Write(T value); }
    interface IBase { }
    interface IDerrived : IBase { }
    interface IAnotherDerrived : IBase { }
    interface ISomethingElse { }

    class DoesDerrived : IReader<IDerrived>
    {
      public IDerrived Read()
      {
        throw new NotImplementedException();
      }
    }

    class DoesStrings : IReader<string>, IWriter<string>
    {
      public string Read() { throw new NotImplementedException(); }
      public void Write(string value) { throw new NotImplementedException(); }
    }
  }
}
