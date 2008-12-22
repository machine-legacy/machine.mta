using System;
using System.Collections.Generic;

using Machine.Mta.Sagas;

namespace Machine.Mta.Internal
{
  public static class InvarianceHelpers
  {
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
    
    public static bool IsImplementationOfGenericType(this Type type, Type required)
    {
      foreach (Type interfaceType in type.FindInterfaces((Type t, object state) => true, null))
      {
        if (interfaceType.IsImplementationOfGenericType(required))
        {
          return true;
        }
      }
      if (type.IsGenericType && type.GetGenericTypeDefinition().Equals(required))
      {
        return true;
      }
      return false;
    }
  }

  public class InvarianceHelpersSpecs
  {
    public void should_all_be_true()
    {
      Console.WriteLine(typeof(IConsume<IMessage>).IsGenericlyCompatible(typeof(ReadsAll)));
      Console.WriteLine(typeof(IConsume<IMessage>).IsGenericlyCompatible(typeof(ReadsSaga)));
      Console.WriteLine(!typeof(IConsume<ISagaMessage>).IsGenericlyCompatible(typeof(ReadsAll)));
      Console.WriteLine(typeof(IConsume<ISagaMessage>).IsGenericlyCompatible(typeof(ReadsSaga)));
      Console.WriteLine(!typeof(IConsume<IAnotherMessage>).IsGenericlyCompatible(typeof(ReadsSaga)));
      Console.WriteLine(typeof(IConsume<IAnotherMessage>).IsGenericlyCompatible(typeof(ReadsAll)));
      Console.WriteLine(typeof(IConsume<IAnotherMessage>).IsGenericlyCompatible(typeof(ReadsAnotherMessages)));
      Console.WriteLine(typeof(IConsume<IMessage>).IsGenericlyCompatible(typeof(ReadsAnotherMessages)));
      Console.WriteLine(!typeof(IConsume<ISagaMessage>).IsGenericlyCompatible(typeof(ReadsAnotherMessages)));
    }

    class ReadsAll : IConsume<IMessage> { public void Consume(IMessage message) { throw new NotImplementedException(); } }
    class ReadsSaga : IConsume<ISagaMessage> { public void Consume(ISagaMessage message) { throw new NotImplementedException(); } }
    class ReadsAnotherMessages : IConsume<IAnotherMessage>{ public void Consume(IAnotherMessage message) { throw new NotImplementedException(); } }
  }
  public interface IAnotherMessage : IMessage
  {
  }
}
