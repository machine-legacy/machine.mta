using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;

using Machine.Mta.Sagas;

namespace Machine.Mta.AdoNet
{
  public abstract class AdoNetSagaStateRepository<T> : ISagaStateRepository<T> where T : class, ISagaState
  {
    readonly BinarySagaSerializer _binarySagaSerializer;

    protected AdoNetSagaStateRepository()
    {
      _binarySagaSerializer = new BinarySagaSerializer();
    }

    public IEnumerable<T> FindAll()
    {
      using (IDbCommand command = CreateSelectAllCommand())
      {
        command.Parameter("SagaType").Value = typeof(T).FullName;
        using (IDataReader reader = command.ExecuteReader())
        {
          List<T> selected = new List<T>();
          while (reader.Read())
          {
            byte[] value = (byte[])reader.GetValue(1);
            selected.Add(_binarySagaSerializer.Deserialize<T>(value));
          }
          reader.Close();
          return selected;
        }
      }
    }

    public void Delete(T sagaState)
    {
      using (IDbCommand command = CreateDeleteCommand())
      {
        command.Parameter("SagaId").Value = sagaState.SagaId;
        command.Parameter("SagaType").Value = typeof(T).FullName;
        command.ExecuteNonQuery();
      }
    }

    public T FindSagaState(Guid sagaId)
    {
      using (IDbCommand command = CreateSelectCommand())
      {
        command.Parameter("SagaId").Value = sagaId;
        command.Parameter("SagaType").Value = typeof(T).FullName;
        using (IDataReader reader = command.ExecuteReader())
        {
          List<T> selected = new List<T>();
          while (reader.Read())
          {
            byte[] value = (byte[])reader.GetValue(0);
            selected.Add(_binarySagaSerializer.Deserialize<T>(value));
          }
          reader.Close();
          return selected.FirstOrDefault();
        }
      }
    }

    public void Add(T sagaState)
    {
      byte[] serialized = _binarySagaSerializer.Serialize(sagaState);
      using (IDbCommand command = CreateInsertCommand())
      {
        command.Parameter("SagaId").Value = sagaState.SagaId;
        command.Parameter("SagaState").Value = serialized;
        command.Parameter("SagaType").Value = typeof (T).FullName;
        if (command.ExecuteNonQuery() != 1)
        {
          throw new SagaStateNotFoundException();
        }
      }
    }

    public void Save(T sagaState)
    {
      byte[] serialized = _binarySagaSerializer.Serialize(sagaState);
      using (IDbCommand command = CreateUpdateCommand())
      {
        command.Parameter("SagaId").Value = sagaState.SagaId;
        command.Parameter("SagaState").Value = serialized;
        command.Parameter("SagaType").Value = typeof (T).FullName;
        if (command.ExecuteNonQuery() != 1)
        {
          throw new SagaStateNotFoundException();
        }
      }
    }

    protected abstract IDbCommand CreateCommand();

    protected virtual string TableName()
    {
      return "saga";
    }

    private IDbCommand CreateInsertCommand()
    {
      IDbCommand command = CreateCommand();
      command.CommandText = "INSERT INTO " + TableName() + " (SagaId, SagaType, SagaState, StartedAt, LastUpdatedAt) VALUES (@SagaId, @SagaType, @SagaState, getutcdate(), getutcdate())";
      command.CreateParameter("SagaType", DbType.String);
      command.CreateParameter("SagaId", DbType.Guid);
      command.CreateParameter("SagaState", DbType.Binary);
      return command;
    }

    private IDbCommand CreateUpdateCommand()
    {
      IDbCommand command = CreateCommand();
      command.CommandText = "UPDATE " + TableName() + " SET SagaState = @SagaState, LastUpdatedAt = getutcdate() WHERE SagaId = @SagaId AND SagaType = @SagaType AND LastUpdatedAt = @LastUpdatedAt";
      command.CreateParameter("SagaType", DbType.String);
      command.CreateParameter("SagaId", DbType.Guid);
      command.CreateParameter("SagaState", DbType.Binary);
      command.CreateParameter("LastUpdatedAt", DbType.DateTime);
      return command;
    }

    private IDbCommand CreateSelectAllCommand()
    {
      IDbCommand command = CreateCommand();
      command.CommandText = "SELECT SagaId, SagaState FROM " + TableName() + " WHERE SagaType = @SagaType";
      command.CreateParameter("SagaType", DbType.String);
      return command;
    }

    private IDbCommand CreateSelectCommand()
    {
      IDbCommand command = CreateCommand();
      command.CommandText = "SELECT SagaState, LastUpdatedAt FROM " + TableName() + " WHERE SagaId = @SagaId AND SagaType = @SagaType";
      command.CreateParameter("SagaId", DbType.Guid);
      command.CreateParameter("SagaType", DbType.String);
      return command;
    }

    private IDbCommand CreateDeleteCommand()
    {
      IDbCommand command = CreateCommand();
      command.CommandText = "DELETE FROM " + TableName() + " WHERE SagaId = @SagaId AND SagaType = @SagaType AND LastUpdatedAt = @LastUpdatedAt";
      command.CreateParameter("SagaId", DbType.Guid);
      command.CreateParameter("SagaType", DbType.String);
      command.CreateParameter("LastUpdatedAt", DbType.DateTime);
      return command;
    }
  }
}
