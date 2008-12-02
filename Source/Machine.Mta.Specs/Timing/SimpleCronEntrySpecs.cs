using System;
using System.Collections.Generic;

using Machine.Mta.Timing;
using Machine.Specifications;

namespace Machine.Mta.Specs.Timing.SimpleCronEntrySpecs
{
  public class with_cron_entry
  {
    protected static SimpleCronEntry entry;
    protected static DateTime occurence;
  }

  [Subject("Cron entry")]
  public class with_entry_with_minute_before : with_cron_entry
  {
    Establish context = () =>
      entry = new SimpleCronEntry(10, null, null, null, null);

    Because of = () =>
      occurence = entry.NextOccurenceAfter(new DateTime(1979, 10, 30, 12, 5, 0));

    It should_have_correct_time = () =>
      occurence.ShouldEqual(new DateTime(1979, 10, 30, 12, 10, 0));
  }

  [Subject("Cron entry")]
  public class with_entry_with_minute_after : with_cron_entry
  {
    Establish context = () =>
      entry = new SimpleCronEntry(10, null, null, null, null);

    Because of = () =>
      occurence = entry.NextOccurenceAfter(new DateTime(1979, 10, 30, 12, 11, 0));

    It should_have_correct_time = () =>
      occurence.ShouldEqual(new DateTime(1979, 10, 30, 13, 10, 0));
  }

  [Subject("Cron entry")]
  public class with_entry_for_hour_before : with_cron_entry
  {
    Establish context = () =>
      entry = new SimpleCronEntry(null, 10, null, null, null);

    Because of = () =>
      occurence = entry.NextOccurenceAfter(new DateTime(1979, 10, 30, 5, 0, 0));

    It should_have_correct_time = () =>
      occurence.ShouldEqual(new DateTime(1979, 10, 30, 10, 0, 0));
  }

  [Subject("Cron entry")]
  public class with_entry_for_hour_after : with_cron_entry
  {
    Establish context = () =>
      entry = new SimpleCronEntry(null, 10, null, null, null);

    Because of = () =>
      occurence = entry.NextOccurenceAfter(new DateTime(1979, 10, 30, 15, 0, 0));

    It should_have_correct_time = () =>
      occurence.ShouldEqual(new DateTime(1979, 10, 31, 10, 0, 0));
  }

  [Subject("Cron entry")]
  public class with_entry_for_day_of_week_before : with_cron_entry
  {
    Establish context = () =>
      entry = new SimpleCronEntry(null, null, null, null, DayOfWeek.Tuesday);

    Because of = () =>
      occurence = entry.NextOccurenceAfter(new DateTime(2008, 11, 24, 1, 1, 0));

    It should_have_correct_time = () =>
      occurence.ShouldEqual(new DateTime(2008, 11, 25, 1, 1, 0));
  }

  [Subject("Cron entry")]
  public class with_entry_for_day_of_week_after : with_cron_entry
  {
    Establish context = () =>
      entry = new SimpleCronEntry(null, null, null, null, DayOfWeek.Tuesday);

    Because of = () =>
      occurence = entry.NextOccurenceAfter(new DateTime(2008, 11, 26, 1, 1, 0));

    It should_have_correct_time_before = () =>
      occurence.ShouldEqual(new DateTime(2008, 12, 2, 1, 1, 0));
  }

  [Subject("Cron entry")]
  public class with_entry_for_day_of_month_before : with_cron_entry
  {
    Establish context = () =>
      entry = new SimpleCronEntry(null, null, 30, null, null);

    Because of = () =>
      occurence = entry.NextOccurenceAfter(new DateTime(2008, 2, 28, 1, 1, 0));

    It should_have_correct_time = () =>
      occurence.ShouldEqual(new DateTime(2008, 3, 30, 1, 1, 0));
  }

  [Subject("Cron entry")]
  public class with_entry_for_day_of_month_after : with_cron_entry
  {
    Establish context = () =>
      entry = new SimpleCronEntry(null, null, 30, null, null);

    Because of = () =>
      occurence = entry.NextOccurenceAfter(new DateTime(2008, 4, 30, 1, 1, 0));

    It should_have_correct_time = () =>
      occurence.ShouldEqual(new DateTime(2008, 5, 30, 1, 1, 0));
  }

  [Subject("Cron entry")]
  public class with_entry_for_month_before : with_cron_entry
  {
    Establish context = () =>
      entry = new SimpleCronEntry(null, null, null, 11, null);

    Because of = () =>
      occurence = entry.NextOccurenceAfter(new DateTime(2008, 10, 30, 1, 1, 0));

    It should_have_correct_time = () =>
      occurence.ShouldEqual(new DateTime(2008, 11, 30, 1, 1, 0));
  }

  [Subject("Cron entry")]
  public class with_entry_for_month_after : with_cron_entry
  {
    Establish context = () =>
      entry = new SimpleCronEntry(null, null, null, 11, null);

    Because of = () =>
      occurence = entry.NextOccurenceAfter(new DateTime(2008, 11, 30, 1, 1, 0));

    It should_have_correct_time = () =>
      occurence.ShouldEqual(new DateTime(2009, 11, 30, 1, 1, 0));
  }

  [Subject("Cron entry")]
  public class with_entry_with_minute_and_previous_occurence_in_the_same_minute : with_cron_entry
  {
    Establish context = () =>
      entry = new SimpleCronEntry(0, null, null, null, null);

    Because of = () =>
      occurence = entry.NextOccurenceAfter(new DateTime(2008, 4, 9, 2, 0, 2));

    It should_have_next_time_after = () =>
      occurence.ShouldEqual(new DateTime(2008, 4, 9, 3, 0, 0));
  }

  [Subject("Cron entry")]
  public class with_entry_with_hour_and_previous_occurence_in_the_same_hour : with_cron_entry
  {
    Establish context = () =>
      entry = new SimpleCronEntry(null, 2, null, null, null);

    Because of = () =>
      occurence = entry.NextOccurenceAfter(new DateTime(2008, 4, 9, 2, 0, 2));

    It should_have_next_time_after = () =>
      occurence.ShouldEqual(new DateTime(2008, 4, 10, 2, 0, 0));
  }

  [Subject("Cron entry")]
  public class with_entry_with_day_and_previous_occurence_in_the_same_day : with_cron_entry
  {
    Establish context = () =>
      entry = new SimpleCronEntry(null, null, 9, null, null);

    Because of = () =>
      occurence = entry.NextOccurenceAfter(new DateTime(2008, 4, 9, 2, 0, 2));

    It should_have_next_time_after = () =>
      occurence.ShouldEqual(new DateTime(2008, 5, 9, 2, 0, 0));
  }

  [Subject("Cron entry")]
  public class with_entry_with_month_and_previous_occurence_in_the_same_month : with_cron_entry
  {
    Establish context = () =>
      entry = new SimpleCronEntry(null, null, null, 4, null);

    Because of = () =>
      occurence = entry.NextOccurenceAfter(new DateTime(2008, 4, 9, 2, 0, 2));

    It should_have_next_time_after = () =>
      occurence.ShouldEqual(new DateTime(2009, 4, 9, 2, 0, 0));
  }

  [Subject("Cron entry")]
  public class with_entry_for_day_of_week_and_previous_occurence_in_desired_day : with_cron_entry
  {
    Establish context = () =>
      entry = new SimpleCronEntry(null, null, null, null, DayOfWeek.Wednesday);

    Because of = () =>
      occurence = entry.NextOccurenceAfter(new DateTime(2008, 4, 9, 2, 0, 2));

    It should_have_next_time_after = () =>
      occurence.ShouldEqual(new DateTime(2008, 4, 16, 2, 0, 0));
  }
}
