using System;
using System.Collections.Generic;

namespace Machine.Mta
{
  public interface IProvideHandlerOrderFor<T> where T : IMessage
  {
    IEnumerable<Type> GetHandlerOrder();
  }
}