using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

using Machine.Container;

namespace Machine.Mta.Timing
{
  public class TimingManager : IStartable, IDisposable
  {
    static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(TimingManager));
    readonly static TimeSpan OnceSecond = TimeSpan.FromSeconds(1.0);
    readonly List<IOnceASecondTask> _tasks = new List<IOnceASecondTask>();
    readonly Thread _thread;
    bool _running = true;

    public TimingManager()
    {
      _thread = new Thread(ThreadMain);
    }

    public void Start()
    {
      _thread.Start();
    }

    public void Add(IOnceASecondTask task)
    {
      if (_thread.IsAlive)
      {
        throw new InvalidOperationException("You must add all once a second tasks before you start the TimingManager.");
      }
      _tasks.Add(task);
    }

    public void Dispose()
    {
      _running = false;
      _thread.Join();
    }

    private void ThreadMain()
    {
      while (_running)
      {
        foreach (IOnceASecondTask task in _tasks)
        {
          try
          {
            task.OnceASecond();
          }
          catch (Exception error)
          {
            _log.Error(error);
          }
        }
        Thread.Sleep(OnceSecond);
      }
    }
  }
}
