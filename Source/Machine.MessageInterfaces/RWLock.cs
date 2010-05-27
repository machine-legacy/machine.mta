using System;
using System.Collections.Generic;
using System.Threading;

namespace Machine.Mta.MessageInterfaces
{
  public delegate bool RwLockGuardCondition();

  public static class RWLock
  {
    public static IDisposable AsWriter(ReaderWriterLock theLock)
    {
      theLock.AcquireWriterLock(Timeout.Infinite);
      return new RWLockWrapper(theLock);
    }

    public static IDisposable AsReader(ReaderWriterLock theLock)
    {
      theLock.AcquireReaderLock(Timeout.Infinite);
      return new RWLockWrapper(theLock);
    }

    public static bool UpgradeToWriterIf(ReaderWriterLock lok, RwLockGuardCondition condition)
    {
      if (condition())
      {
        lok.UpgradeToWriterLock(Timeout.Infinite);
        if (condition())
        {
          return true;
        }
      }
      return false;
    }
  }

  public class RWLockWrapper : IDisposable
  {
    private readonly ReaderWriterLock _readerWriterLock;

    public RWLockWrapper(ReaderWriterLock readerWriterLock)
    {
      _readerWriterLock = readerWriterLock;
    }

    public void Dispose()
    {
      _readerWriterLock.ReleaseLock();
    }
  }
}