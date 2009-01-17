using System;
using System.Collections.Generic;

namespace Machine.Mta
{
  public interface IMessageFactory
  {
    IMessage Create(Type type, params object[] parameters);
    T Create<T>() where T : class, IMessage;
    T Create<T>(object value) where T : class, IMessage;
  }
}
