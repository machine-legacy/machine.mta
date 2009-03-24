using System;
using System.Collections.Generic;

namespace Machine.Mta
{
  [AttributeUsage(AttributeTargets.Class, AllowMultiple=false)]
  public class RetryAttemptsAttribute : Attribute
  {
    readonly Int32 _numberOfTries;

    public Int32 NumberOfTries
    {
      get { return _numberOfTries; }
    }

    public RetryAttemptsAttribute(Int32 numberOfTries)
    {
      if (_numberOfTries < 0) throw new ArgumentException("numberOfTries");
      _numberOfTries = numberOfTries;
    }
  }
}
