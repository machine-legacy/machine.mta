using System;
using System.Collections.Generic;
using System.IO;

using Machine.Mta.Timing;

namespace Machine.Mta.DotNetBinaryStorage
{
  public class DotNetBinaryScheduledPublishRepository : IScheduledPublishRepository
  {
    readonly IFlatFileSystem _flatFileSystem;
    readonly IFlatBinaryFileConfiguration _configuration;
    List<ScheduledPublish> _cache;

    public DotNetBinaryScheduledPublishRepository(IFlatBinaryFileConfiguration configuration, IFlatFileSystem flatFileSystem)
    {
      _configuration = configuration;
      _flatFileSystem = flatFileSystem;
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
      if (_flatFileSystem.IsFile(_configuration.ScheduledPublishesPath))
      {
        using (Stream stream = _flatFileSystem.Open(_configuration.ScheduledPublishesPath))
        {
          _cache.AddRange((ScheduledPublish[])Serializers.Binary.Deserialize(stream));
        }
      }
    }

    private void WriteToDisk()
    {
      using (Stream stream = _flatFileSystem.Create(_configuration.ScheduledPublishesPath))
      {
        Serializers.Binary.Serialize(stream, _cache.ToArray());
      }
    }
  }
}