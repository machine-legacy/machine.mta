using System;
using System.Collections.Generic;

namespace Machine.Mta
{
  public interface IProvideHandlerOrderFor<T> : IProvideHandlerOrder where T : IMessage
  {
  }

  public interface IProvideHandlerOrder
  {
    IEnumerable<Type> GetHandlerOrder();
  }
}