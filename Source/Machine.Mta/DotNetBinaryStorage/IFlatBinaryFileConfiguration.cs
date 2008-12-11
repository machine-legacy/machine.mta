using System;

namespace Machine.Mta.DotNetBinaryStorage
{
  public interface IFlatBinaryFileConfiguration
  {
    string ScheduledPublishesPath { get; }
    string SagaStatePath { get; }
  }
  public class StaticFlatBinaryFileConfiguration : IFlatBinaryFileConfiguration
  {
    private readonly string _scheduledPublishesPath;
    private readonly string _sagaStatePath;

    public string ScheduledPublishesPath
    {
      get { return _scheduledPublishesPath; }
    }

    public string SagaStatePath
    {
      get { return _sagaStatePath; }
    }

    public StaticFlatBinaryFileConfiguration(string scheduledPublishesPath, string sagaStatePath)
    {
      _scheduledPublishesPath = scheduledPublishesPath;
      _sagaStatePath = sagaStatePath;
    }
  }
}
