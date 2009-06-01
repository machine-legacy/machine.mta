using System;
using System.Collections.Generic;

namespace Machine.Mta
{
  public interface IMessageFactory
  {
    IMessage Create(Type type, params object[] parameters);
    T Create<T>() where T : IMessage;
    T Create<T>(object value) where T : IMessage;
    T Create<T>(Action<T> factory) where T : IMessage;
  }
}
