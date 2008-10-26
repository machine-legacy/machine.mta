using System;
using System.Collections.Generic;

namespace Machine.Mta.Minimalistic
{
  public static class CovarianceHelpers
  {
    // I make no promises to the correctness of this! Hell no!
    public static bool IsCovariantWith(this Type type1, Type type2)
    {
      foreach (Type interfaceType in type2.GetInterfaces())
      {
        if (type1.IsCovariantWith(interfaceType))
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
  
  public class CovarianceSpecs
  {
    public void should_all_be_true()
    {
      Console.WriteLine(typeof(IReader<string>).IsCovariantWith(typeof(DoesStrings)));
      Console.WriteLine(typeof(IReader<object>).IsCovariantWith(typeof(DoesStrings)));
      Console.WriteLine(typeof(IWriter<string>).IsCovariantWith(typeof(DoesStrings)));
      Console.WriteLine(typeof(IWriter<object>).IsCovariantWith(typeof(DoesStrings)));
      Console.WriteLine(!typeof(IWriter<short>).IsCovariantWith(typeof(DoesStrings)));
      Console.WriteLine(typeof(IReader<IBase>).IsCovariantWith(typeof(DoesDerrived)));
      Console.WriteLine(typeof(IReader<IDerrived>).IsCovariantWith(typeof(DoesDerrived)));
      Console.WriteLine(typeof(IReader<object>).IsCovariantWith(typeof(DoesDerrived)));
      Console.WriteLine(!typeof(IReader<ISomethingElse>).IsCovariantWith(typeof(DoesDerrived)));
      Console.WriteLine(typeof(IReader<object>).IsCovariantWith(typeof(IReader<string>)));
      Console.WriteLine(!typeof(IReader<Int32>).IsCovariantWith(typeof(IReader<string>)));
    }

    interface IReader<T> { T Read(); }
    interface IWriter<T> { void Write(T value); }
    interface IBase { }
    interface IDerrived : IBase { }
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
