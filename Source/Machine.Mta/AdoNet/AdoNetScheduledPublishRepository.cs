using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;

using Machine.Mta.Timing;

namespace Machine.Mta.AdoNet
{
  public class AdoNetScheduledPublishRepository : IScheduledPublishRepository
  {
    readonly IAdoNetConnectionString _connectionString;

    public AdoNetScheduledPublishRepository(IAdoNetConnectionString connectionString)
    {
      _connectionString = connectionString;
    }

    private IDbConnection OpenConnection()
    {
      IDbConnection connection = new SqlConnection(_connectionString.ConnectionString);
      connection.Open();
      return connection;
    }

    public void Add(ScheduledPublish scheduled)
    {
      using (IDbConnection connection = OpenConnection())
      {
        foreach (EndpointName destination in scheduled.Addresses)
        {
          IDbCommand command = CreateInsertCommand(connection);
          command.Parameter("PublishAt").Value = scheduled.PublishAt;
          command.Parameter("ReturnAddress").Value = destination.ToString();
          command.Parameter("MessagePayload").Value = scheduled.MessagePayload.ToByteArray();
          command.Parameter("SagaId").Value = scheduled.SagaId;
          if (command.ExecuteNonQuery() != 1)
          {
            throw new InvalidOperationException();
          }
        }
      }
    }

    public ICollection<ScheduledPublish> FindAllExpired()
    {
      using (IDbConnection connection = OpenConnection())
      {
        List<ScheduledPublish> scheduled = new List<ScheduledPublish>();
        DateTime now = ServerClock.Now();
        IDbCommand command = CreateSelectCommand(connection);
        command.Parameter("Now").Value = now;
        using (IDataReader reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            DateTime publishAt = reader.GetDateTime(1);
            string returnAddress = reader.GetString(2);
            byte[] messagePayload = (byte[])reader.GetValue(3);
            Guid sagaId = (Guid)reader.GetValue(4);
            scheduled.Add(new ScheduledPublish(publishAt, new MessagePayload(messagePayload), new[] { EndpointName.FromString(returnAddress) }, sagaId));
          }
          reader.Close();
        }
        IDbCommand delete = CreateDeleteCommand(connection);
        delete.Parameter("Now").Value = now;
        delete.ExecuteNonQuery();
        return scheduled;
      }
    }

    private static IDbCommand CreateInsertCommand(IDbConnection connection)
    {
      IDbCommand command = connection.CreateCommand();
      command.Connection = connection;
      command.CommandText = "INSERT INTO future_publish (CreatedAt, PublishAt, ReturnAddress, MessagePayload, SagaId) VALUES (getutcdate(), @PublishAt, @ReturnAddress, @MessagePayload, @SagaId)";
      command.CreateParameter("PublishAt", DbType.DateTime);
      command.CreateParameter("ReturnAddress", DbType.String);
      command.CreateParameter("MessagePayload", DbType.Binary);
      command.CreateParameter("SagaId", DbType.Guid);
      return command;
    }

    private static IDbCommand CreateSelectCommand(IDbConnection connection)
    {
      IDbCommand command = connection.CreateCommand();
      command.Connection = connection;
      command.CommandText = "SELECT Id, PublishAt, ReturnAddress, MessagePayload, SagaId FROM future_publish WHERE PublishAt < @Now";
      command.CreateParameter("Now", DbType.DateTime);
      return command;
    }

    private static IDbCommand CreateDeleteCommand(IDbConnection connection)
    {
      IDbCommand command = connection.CreateCommand();
      command.Connection = connection;
      command.CommandText = "DELETE FROM future_publish WHERE PublishAt < @Now";
      command.CreateParameter("Now", DbType.DateTime);
      return command;
    }
  }
}