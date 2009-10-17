using System;
using System.Collections.Generic;

namespace Machine.Mta.Timing
{
  public class InMemoryScheduledPublishRepository : IScheduledPublishRepository
  {
    readonly List<ScheduledPublish> _scheduled = new List<ScheduledPublish>();
    readonly object _lock = new object();

    public void Clear()
    {
      lock (_lock)
      {
        _scheduled.Clear();
      }
    }

    public void Add(ScheduledPublish scheduled)
    {
      lock (_lock)
      {
        _scheduled.Add(scheduled);
      }
    }

    public ICollection<ScheduledPublish> FindAllExpired()
    {
      lock (_lock)
      {
        var now = ServerClock.Now;
        var expired = new List<ScheduledPublish>();
        foreach (var schedule in _scheduled.ToArray())
        {
          if (schedule.PublishAt < now)
          {
            expired.Add(schedule);
            _scheduled.Remove(schedule);
          }
        }
        return expired;
      }
    }
  }
}