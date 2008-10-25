using System;
using System.Collections.Generic;

namespace Machine.Mta.InterfacesAsMessages
{
  public interface IMessageFactory
  {
    IMessage Create(Type type);
    T Create<T>() where T : class, IMessage;
  }
}
