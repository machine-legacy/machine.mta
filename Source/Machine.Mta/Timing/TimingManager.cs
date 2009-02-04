using System;
using System.Collections.Generic;
using System.Threading;

using Machine.Container;

namespace Machine.Mta.Timing
{
  public class TimingManager : IStartable, IDisposable
  {
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
          task.OnceASecond();
        }
        Thread.Sleep(OnceSecond);
      }
    }
  }
}
