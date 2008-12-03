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

    public void Delete(T sagaState)
    {
      using (IDbConnection connection = OpenConnection())
      {
        IDbCommand command = CreateDeleteCommand(connection);
        command.Parameter("SagaId").Value = sagaState.SagaId;
        command.ExecuteNonQuery();
      }
    }

    public T FindSagaState(Guid sagaId)
    {
      using (IDbConnection connection = OpenConnection())
      {
        IDbCommand command = CreateSelectCommand(connection);
        command.Parameter("SagaId").Value = sagaId;
        using (IDataReader reader = command.ExecuteReader())
        {
          List<T> selected = new List<T>();
          while (reader.Read())
          {
            byte[] value = (byte[])reader.GetValue(0);
            selected.Add(_binarySagaSerializer.Deserialize<T>(value));
          }
          reader.Close();
          return selected.First();
        }
      }
    }

    public void Save(T sagaState)
    {
      using (IDbConnection connection = OpenConnection())
      {
        byte[] serialized = _binarySagaSerializer.Serialize(sagaState);
        bool success = false;
        foreach (IDbCommand command in new IDbCommand[] { CreateUpdateCommand(connection), CreateInsertCommand(connection) })
        {
          command.Parameter("SagaId").Value = sagaState.SagaId;
          command.Parameter("SagaState").Value = serialized;
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
      command.CommandText = "INSERT INTO saga (SagaId, SagaState, StartedAt, LastUpdatedAt) VALUES (@SagaId, @SagaState, getutcdate(), getutcdate())";
      command.CreateParameter("SagaId", DbType.Guid);
      command.CreateParameter("SagaState", DbType.Binary);
      return command;
    }

    private static IDbCommand CreateUpdateCommand(IDbConnection connection)
    {
      IDbCommand command = connection.CreateCommand();
      command.Connection = connection;
      command.CommandText = "UPDATE saga SET SagaState = @SagaState, LastUpdatedAt = getutcdate() WHERE SagaId = @SagaId";
      command.CreateParameter("SagaId", DbType.Guid);
      command.CreateParameter("SagaState", DbType.Binary);
      return command;
    }

    private static IDbCommand CreateSelectCommand(IDbConnection connection)
    {
      IDbCommand command = connection.CreateCommand();
      command.Connection = connection;
      command.CommandText = "SELECT SagaState FROM saga WHERE SagaId = @SagaId";
      command.CreateParameter("SagaId", DbType.Guid);
      return command;
    }

    private static IDbCommand CreateDeleteCommand(IDbConnection connection)
    {
      IDbCommand command = connection.CreateCommand();
      command.Connection = connection;
      command.CommandText = "DELETE FROM saga WHERE SagaId = @SagaId";
      command.CreateParameter("SagaId", DbType.Guid);
      return command;
    }
  }
}
