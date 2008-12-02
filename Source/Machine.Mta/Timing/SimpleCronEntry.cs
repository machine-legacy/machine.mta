using System;
using System.Collections.Generic;

namespace Machine.Mta.Timing
{
  public class SimpleCronEntry
  {
    private readonly short? _dayOfMonth;
    private readonly DayOfWeek? _dayOfWeek;
    private readonly short? _hour;
    private readonly short? _minute;
    private readonly short? _month;

    public SimpleCronEntry(short? minute, short? hour, short? dayOfMonth, short? month, DayOfWeek? dayOfWeek)
    {
      _minute = minute;
      _hour = hour;
      _dayOfMonth = dayOfMonth;
      _month = month;
      _dayOfWeek = dayOfWeek;
    }

    public short? DayOfMonth
    {
      get { return _dayOfMonth; }
    }

    public DayOfWeek? DayOfWeek
    {
      get { return _dayOfWeek; }
    }

    public short? Hour
    {
      get { return _hour; }
    }

    public short? Minute
    {
      get { return _minute; }
    }

    public short? Month
    {
      get { return _month; }
    }

    public DateTime NextOccurenceAfter(DateTime time)
    {
      DateTime nextOccurence = time.TruncateSeconds();
      nextOccurence = SetNextMinute(nextOccurence);
      nextOccurence = SetNextHour(nextOccurence);
      nextOccurence = SetNextDayOfWeek(nextOccurence);
      nextOccurence = SetNextDayOfMonth(nextOccurence);
      nextOccurence = SetNextMonth(nextOccurence);
      return nextOccurence;
    }

    private DateTime SetNextDayOfMonth(DateTime occurrence)
    {
      DateTime tempDate = occurrence;
      if (_dayOfMonth.HasValue)
      {
        while (DayOfMonth.Value > DateTime.DaysInMonth(tempDate.Year, tempDate.Month))
        {
          tempDate = tempDate.AddDays(-(tempDate.Day - 1));
          tempDate = tempDate.AddMonths(1);
        }
        tempDate = tempDate.AddDays(_dayOfMonth.Value - tempDate.Day);
        if (tempDate < occurrence || tempDate.Day == occurrence.Day)
        {
          return tempDate.AddMonths(1);
        }
      }
      return tempDate;
    }

    private DateTime SetNextDayOfWeek(DateTime occurrence)
    {
      DateTime tempDate = occurrence;
      if (_dayOfWeek.HasValue)
      {
        tempDate = occurrence.AddDays((int)_dayOfWeek.Value - (int)occurrence.DayOfWeek);
        if (tempDate < occurrence || tempDate.DayOfWeek == occurrence.DayOfWeek)
        {
          return tempDate.AddDays(7);
        }
      }
      return tempDate;
    }

    private DateTime SetNextHour(DateTime occurrence)
    {
      DateTime tempDate = occurrence;
      if (_hour.HasValue)
      {
        tempDate = occurrence.AddHours(_hour.Value - occurrence.Hour);
        if (tempDate < occurrence || tempDate.Hour == occurrence.Hour)
        {
          return tempDate.AddDays(1);
        }
      }
      return tempDate;
    }

    private DateTime SetNextMinute(DateTime occurrence)
    {
      DateTime tempDate = occurrence;
      if (_minute.HasValue)
      {
        tempDate = occurrence.AddMinutes(_minute.Value - occurrence.Minute);
        if (tempDate < occurrence || tempDate.Minute == occurrence.Minute)
        {
          return tempDate.AddHours(1);
        }
      }
      return tempDate;
    }

    private DateTime SetNextMonth(DateTime occurrence)
    {
      DateTime tempDate = occurrence;
      if (_month.HasValue)
      {
        tempDate = occurrence.AddMonths(_month.Value - occurrence.Month);
        if (tempDate < occurrence || tempDate.Month == occurrence.Month)
        {
          return tempDate.AddYears(1);
        }
      }
      return tempDate;
    }
  }
  public static class DateTimeHelpers
  {
    public static DateTime TruncateSeconds(this DateTime time)
    {
      return new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0);
    }
  }
}
