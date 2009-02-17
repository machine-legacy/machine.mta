using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using System.Data;

using Machine.Mta.Sagas;

namespace Machine.Mta.AdoNet
{
  public class AdoNetSagaStateRepository<T> : ISagaStateRepository<T> where T : class, ISagaState
  {
    readonly IAdoNetConnectionString _connectionString;
    readonly BinarySagaSerializer _binarySagaSerializer;

    public AdoNetSagaStateRepository(IAdoNetConnectionString connectionString)
    {
      _binarySagaSerializer = new BinarySagaSerializer();
      _connectionString = connectionString;
    }

    private IDbConnection OpenConnection()
    {
      IDbConnection connection = new SqlConnection(_connectionString.ConnectionString);
      connection.Open();
      return connection;
    }

    public IEnumerable<T> FindAll()
    {
      using (IDbConnection connection = OpenConnection())
      {
        IDbCommand command = CreateSelectAllCommand(connection);
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
      using (IDbConnection connection = OpenConnection())
      {
        IDbCommand command = CreateDeleteCommand(connection);
        command.Parameter("SagaId").Value = sagaState.SagaId;
        command.Parameter("SagaType").Value = typeof(T).FullName;
        command.ExecuteNonQuery();
      }
    }

    public T FindSagaState(Guid sagaId)
    {
      using (IDbConnection connection = OpenConnection())
      {
        IDbCommand command = CreateSelectCommand(connection);
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

    public void Save(T sagaState)
    {
      using (IDbConnection connection = OpenConnection())
      {
        byte[] serialized = _binarySagaSerializer.Serialize(sagaState);
        bool success = false;
        foreach (IDbCommand command in new [] { CreateUpdateCommand(connection), CreateInsertCommand(connection) })
        {
          command.Parameter("SagaId").Value = sagaState.SagaId;
          command.Parameter("SagaState").Value = serialized;
          command.Parameter("SagaType").Value = typeof(T).FullName;
          if (command.ExecuteNonQuery() == 1)
          {
            success = true;
            break;
          }
        }
        if (!success)
        {
          throw new SagaStateNotFoundException();
        }
      }
    }

    private static IDbCommand CreateInsertCommand(IDbConnection connection)
    {
      IDbCommand command = connection.CreateCommand();
      command.Connection = connection;
      command.CommandText = "INSERT INTO saga (SagaId, SagaType, SagaState, StartedAt, LastUpdatedAt) VALUES (@SagaId, @SagaType, @SagaState, getutcdate(), getutcdate())";
      command.CreateParameter("SagaType", DbType.String);
      command.CreateParameter("SagaId", DbType.Guid);
      command.CreateParameter("SagaState", DbType.Binary);
      return command;
    }

    private static IDbCommand CreateUpdateCommand(IDbConnection connection)
    {
      IDbCommand command = connection.CreateCommand();
      command.Connection = connection;
      command.CommandText = "UPDATE saga SET SagaState = @SagaState, LastUpdatedAt = getutcdate() WHERE SagaId = @SagaId AND SagaType = @SagaType";
      command.CreateParameter("SagaType", DbType.String);
      command.CreateParameter("SagaId", DbType.Guid);
      command.CreateParameter("SagaState", DbType.Binary);
      return command;
    }

    private static IDbCommand CreateSelectAllCommand(IDbConnection connection)
    {
      IDbCommand command = connection.CreateCommand();
      command.Connection = connection;
      command.CommandText = "SELECT SagaId, SagaState FROM saga WHERE SagaType = @SagaType";
      command.CreateParameter("SagaType", DbType.String);
      return command;
    }

    private static IDbCommand CreateSelectCommand(IDbConnection connection)
    {
      IDbCommand command = connection.CreateCommand();
      command.Connection = connection;
      command.CommandText = "SELECT SagaState FROM saga WHERE SagaId = @SagaId AND SagaType = @SagaType";
      command.CreateParameter("SagaId", DbType.Guid);
      command.CreateParameter("SagaType", DbType.String);
      return command;
    }

    private static IDbCommand CreateDeleteCommand(IDbConnection connection)
    {
      IDbCommand command = connection.CreateCommand();
      command.Connection = connection;
      command.CommandText = "DELETE FROM saga WHERE SagaId = @SagaId AND SagaType = @SagaType";
      command.CreateParameter("SagaId", DbType.Guid);
      command.CreateParameter("SagaType", DbType.String);
      return command;
    }
  }
}
