using System;
using System.Collections.Generic;
using System.Transactions;

namespace Machine.Mta
{
  public interface ITransactionManager
  {
    TransactionScope CreateTransactionScope();
  }
  public class TransactionManager : ITransactionManager
  {
    public TransactionScope CreateTransactionScope()
    {
      return new TransactionScope();
    }
  }
}
