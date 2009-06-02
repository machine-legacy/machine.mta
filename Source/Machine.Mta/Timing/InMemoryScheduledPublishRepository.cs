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
        DateTime now = ServerClock.Now();
        List<ScheduledPublish> expired = new List<ScheduledPublish>();
        foreach (ScheduledPublish schedule in new List<ScheduledPublish>(_scheduled))
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