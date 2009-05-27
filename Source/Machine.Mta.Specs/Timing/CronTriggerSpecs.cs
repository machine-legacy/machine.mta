using System;
using System.Collections.Generic;

using Machine.Mta.Timing;
using Machine.Specifications;

namespace Machine.Mta.Specs.Timing.CronTriggerSpecs
{
  public class with_cron_trigger
  {
    protected static CronTrigger trigger;
    protected static bool triggered;

    Establish context = () =>
    {
      ServerClock.Now = (() => new DateTime(2008, 1, 1, 10, 10, 10));
      trigger = CronTriggerFactory.EveryHalfHour();
    };

    Because of = () =>
      triggered = trigger.IsFired();
  }

  [Subject("Cron trigger")]
  public class with_cron_trigger_not_at_trigger_time : with_cron_trigger
  {
    Establish context = () =>
      ServerClock.Now = (() => new DateTime(2008, 1, 1, 10, 15, 10));

    It should_not_trigger = () =>
      triggered.ShouldBeFalse();
  }

  [Subject("Cron trigger")]
  public class with_cron_trigger_at_trigger_time : with_cron_trigger
  {
    Establish context = () =>
      ServerClock.Now = (() => new DateTime(2008, 1, 1, 10, 30, 1));

    It should_fire = () =>
      triggered.ShouldBeTrue();

    It should_fire_only_once = () =>
      trigger.IsFired().ShouldBeFalse();
  }
}
