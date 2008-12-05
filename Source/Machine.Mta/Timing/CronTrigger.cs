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
      _lastRanAt = ServerClock.Now();
    }

    public bool IsFired()
    {
      foreach (SimpleCronEntry entry in _entries)
      {
        if (entry.NextOccurenceAfter(_lastRanAt) < ServerClock.Now())
        {
          _lastRanAt = ServerClock.Now();
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
}
