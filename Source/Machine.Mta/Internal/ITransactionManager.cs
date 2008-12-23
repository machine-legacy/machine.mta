using System;
using System.Collections.Generic;
using System.Transactions;

namespace Machine.Mta.Internal
{
  public interface ITransactionManager
  {
    TransactionScope CreateTransactionScope();
  }
}
