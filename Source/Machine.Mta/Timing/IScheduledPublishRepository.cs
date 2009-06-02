using System.Collections.Generic;

namespace Machine.Mta.Timing
{
  public interface IScheduledPublishRepository
  {
    void Clear();
    void Add(ScheduledPublish scheduled);
    ICollection<ScheduledPublish> FindAllExpired();
  }
}