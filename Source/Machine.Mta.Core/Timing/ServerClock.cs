using System;
using System.Collections.Generic;

namespace Machine.Mta.Timing
{
  public class ServerClock
  {
    static Func<DateTime> _nowFunc;

    public static void SetNowFunc(Func<DateTime> nowFunc)
    {
      _nowFunc = nowFunc;
    }

    public static void ResetNowFunc()
    {
      _nowFunc = null;
    }

    public static DateTime Now
    {
      get
      {
        if (_nowFunc == null)
          throw new InvalidOperationException("No Now function has been assigned to the ServerClock. Please see ServerClock.SetNowFunc");
        return _nowFunc();
      }
    }

    public static DateTime Later(TimeSpan time)
    {
      return Now + time;
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
