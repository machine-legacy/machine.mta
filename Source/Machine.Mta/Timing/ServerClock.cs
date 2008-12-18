using System;
using System.Collections.Generic;

namespace Machine.Mta.Timing
{
  public class ServerClock
  {
    public static Func<DateTime> Now = () => DateTime.UtcNow;

    public static DateTime Later(TimeSpan time)
    {
      return Now() + time;
    }

    public static DateTime SecondsLater(double seconds)
    {
      return Later(TimeSpan.FromSeconds(seconds));
    }

    public static DateTime MinutesLater(double minutes)
    {
      return Later(TimeSpan.FromMinutes(minutes));
    }
  }
}
