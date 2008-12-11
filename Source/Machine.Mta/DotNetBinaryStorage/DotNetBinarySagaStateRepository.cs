using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using Machine.Mta.Sagas;

namespace Machine.Mta.DotNetBinaryStorage
{
  public abstract class DotNetBinarySagaStateRepository<T> : ISagaStateRepository<T> where T : class, ISagaState
  {
    readonly BinaryFormatter _formatter = new BinaryFormatter();
    readonly IFlatBinaryFileConfiguration _configuration;

    protected DotNetBinarySagaStateRepository(IFlatBinaryFileConfiguration configuration)
    {
      _configuration = configuration;
    }

    public T FindSagaState(Guid sagaId)
    {
      string path = PathForState(sagaId);
      if (!File.Exists(path))
      {
        return default(T);
      }
      using (FileStream stream = File.OpenRead(path))
      {
        return (T)_formatter.Deserialize(stream);
      }
    }

    public void Save(T sagaState)
    {
      string path = PathForState(sagaState.SagaId);
      using (FileStream stream = File.Create(path))
      {
        _formatter.Serialize(stream, sagaState);
      }
    }

    public void Delete(T sagaState)
    {
      string path = PathForState(sagaState.SagaId);
      if (File.Exists(path))
      {
        File.Delete(path);
      }
    }

    public string PathForState(Guid id)
    {
      return Path.Combine(_configuration.SagaStatePath, id.ToString("D") + "." + Suffix);
    }

    public abstract string Suffix { get; }
  }
}
