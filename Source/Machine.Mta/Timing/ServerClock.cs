using System;
using System.Collections.Generic;

namespace Machine.Mta.Timing
{
  public class ServerClock
  {
    public static Func<DateTime> Now = () => DateTime.UtcNow;
  }
}
