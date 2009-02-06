using System;
using System.Collections.Generic;
using System.Transactions;

namespace Machine.Mta
{
  public interface ITransactionManager
  {
    TransactionScope CreateTransactionScope();
  }
}
