using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
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
          command.Parameter("PublishId").Value = scheduled.Id;
          command.Parameter("PublishAt").Value = scheduled.PublishAt;
          command.Parameter("ReturnAddress").Value = destination.ToString();
          command.Parameter("MessagePayload").Value = scheduled.MessagePayload.MakeString();
          command.Parameter("SagaIds").Value = scheduled.SagaIds.MakeString();
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
            DateTime publishAt = reader.GetDateTime(2);
            string returnAddress = reader.GetString(3);
            string messagePayload = reader.GetString(4);
            string sagaIds = reader.GetString(5);
            scheduled.Add(new ScheduledPublish(publishAt, MessagePayload.FromString(messagePayload), new[] { EndpointName.FromString(returnAddress) }, sagaIds.ToGuidArray()));
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
      command.CommandText = "INSERT INTO future_publish (CreatedAt, PublishId, PublishAt, ReturnAddress, MessagePayload, SagaIds) VALUES (getutcdate(), @PublishId, @PublishAt, @ReturnAddress, @MessagePayload, @SagaIds)";
      command.CreateParameter("PublishId", DbType.Guid);
      command.CreateParameter("PublishAt", DbType.DateTime);
      command.CreateParameter("ReturnAddress", DbType.String);
      command.CreateParameter("MessagePayload", DbType.String);
      command.CreateParameter("SagaIds", DbType.String);
      return command;
    }

    private static IDbCommand CreateSelectCommand(IDbConnection connection)
    {
      IDbCommand command = connection.CreateCommand();
      command.Connection = connection;
      command.CommandText = "SELECT Id, PublishId, PublishAt, ReturnAddress, MessagePayload, SagaIds FROM future_publish WHERE PublishAt < @Now";
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
  public static class MappingHelpers
  {
    public static string MakeString(this Guid[] ids)
    {
      return MakeString(ids);
    }

    public static Guid[] ToGuidArray(this string vaule)
    {
      return MakeObject<Guid[]>(vaule);
    }

    public static string MakeString(object obj)
    {
      using (MemoryStream stream = new MemoryStream())
      {
        Serializers.Binary.Serialize(stream, obj);
        return Convert.ToBase64String(stream.ToArray());
      }
    }

    public static T MakeObject<T>(string value)
    {
      using (MemoryStream stream = new MemoryStream(Convert.FromBase64String(value)))
      {
        return (T)Serializers.Binary.Deserialize(stream);
      }
    }
  }
}