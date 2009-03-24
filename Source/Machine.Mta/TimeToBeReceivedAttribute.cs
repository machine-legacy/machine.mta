using System;
using System.Collections.Generic;

namespace Machine.Mta
{
  [AttributeUsage(AttributeTargets.Class, AllowMultiple=false)]
  public class TimeToBeReceivedAttribute : Attribute
  {
    readonly TimeSpan _timeToBeReceived;

    public TimeSpan TimeToBeReceived
    {
      get { return _timeToBeReceived; }
    }

    public TimeToBeReceivedAttribute(TimeSpan timeToBeReceived)
    {
      if (timeToBeReceived < TimeSpan.Zero) throw new ArgumentException("timeToBeReceived");
      _timeToBeReceived = timeToBeReceived;
    }
  }
}