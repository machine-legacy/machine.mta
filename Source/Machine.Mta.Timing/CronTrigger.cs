using System;
using System.Collections.Generic;

namespace Machine.Mta.Timing
{
  public static class CronTriggerFactory
  {
    public static CronTrigger EveryMinute()
    {
      List<SimpleCronEntry> entries = new List<SimpleCronEntry>();
      for (short i = 0; i < 60; ++i)
      {
        entries.Add(new SimpleCronEntry(i, null, null, null, null));
      }
      return entries.ToTrigger();
    }
    
    public static CronTrigger EveryFiveMinutes()
    {
      List<SimpleCronEntry> entries = new List<SimpleCronEntry>();
      for (short i = 0; i < 60; i += 5)
      {
        entries.Add(new SimpleCronEntry(i, null, null, null, null));
      }
      return entries.ToTrigger();
    }
    
    public static CronTrigger EveryHalfHour()
    {
      List<SimpleCronEntry> entries = new List<SimpleCronEntry>();
      entries.Add(new SimpleCronEntry( 0, null, null, null, null));
      entries.Add(new SimpleCronEntry(30, null, null, null, null));
      return entries.ToTrigger();
    }

    public static ITrigger EverySecond()
    {
      return new AlwaysTriggered();
    }

    public static ITrigger EveryThirtySeconds()
    {
      return EveryNSeconds(30);
    }

    public static ITrigger EveryTenSeconds()
    {
      return EveryNSeconds(10);
    }

    public static ITrigger EveryNSeconds(Int32 seconds)
    {
      return new EveryNSeconds(seconds);
    }

    public static CronTrigger ToTrigger(this List<SimpleCronEntry> entries)
    {
      return new CronTrigger(entries.ToArray());
    }
  }
  public class CronTrigger : ITrigger
  {
    readonly SimpleCronEntry[] _entries;
    DateTime _lastRanAt;

    public CronTrigger(SimpleCronEntry[] entries)
    {
      _entries = entries;
      _lastRanAt = ServerClock.Now;
    }

    public bool IsFired()
    {
      foreach (SimpleCronEntry entry in _entries)
      {
        var nextOccurenceAfter = entry.NextOccurenceAfter(_lastRanAt);
        if (nextOccurenceAfter < ServerClock.Now)
        {
          _lastRanAt = ServerClock.Now;
          return true;
        }
      }
      return false;
    }
  }
  public class AlwaysTriggered : ITrigger
  {
    public bool IsFired()
    {
      return true;
    }
  }
  public class EveryNSeconds : ITrigger
  {
    readonly Int32 _interval;

    public EveryNSeconds(Int32 interval)
    {
      _interval = interval;
    }

    public bool IsFired()
    {
      return ServerClock.Now.Second % _interval == 0;
    }
  }
}
