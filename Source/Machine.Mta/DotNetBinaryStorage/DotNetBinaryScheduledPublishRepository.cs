using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

using Machine.Mta.Timing;

namespace Machine.Mta.DotNetBinaryStorage
{
  public class DotNetBinaryScheduledPublishRepository : IScheduledPublishRepository
  {
    readonly BinaryFormatter _formatter = new BinaryFormatter();
    readonly IFlatBinaryFileConfiguration _configuration;
    List<ScheduledPublish> _cache;

    public DotNetBinaryScheduledPublishRepository(IFlatBinaryFileConfiguration configuration)
    {
      _configuration = configuration;
    }

    public void Add(ScheduledPublish scheduled)
    {
      ReadFromDisk();
      _cache.Add(scheduled);
      WriteToDisk();
    }

    public ICollection<ScheduledPublish> FindAllExpired()
    {
      ReadFromDisk();
      List<ScheduledPublish> expired = new List<ScheduledPublish>();
      foreach (ScheduledPublish publish in new List<ScheduledPublish>(_cache))
      {
        if (publish.PublishAt < ServerClock.Now())
        {
          expired.Add(publish);
          _cache.Remove(publish);
        }
      }
      if (expired.Count > 0)
      {
        WriteToDisk();
      }
      return expired;
    }

    private void ReadFromDisk()
    {
      if (_cache != null)
      {
        return;
      }
      _cache = new List<ScheduledPublish>();
      if (File.Exists(_configuration.ScheduledPublishesPath))
      {
        using (FileStream stream = File.OpenRead(_configuration.ScheduledPublishesPath))
        {
          _cache.AddRange((ScheduledPublish[]) _formatter.Deserialize(stream));
        }
      }
    }

    private void WriteToDisk()
    {
      using (FileStream stream = File.Create(_configuration.ScheduledPublishesPath))
      {
        _formatter.Serialize(stream, _cache.ToArray());
      }
    }
  }
}