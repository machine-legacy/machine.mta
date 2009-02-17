using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Linq;

using Machine.Mta.Timing;
using Machine.Mta.Sagas;

namespace Machine.Mta.AdoNet
{
  public class AdoNetScheduledPublishRepository : IScheduledPublishRepository
  {
    static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(AdoNetScheduledPublishRepository));
    readonly IAdoNetConnectionString _connectionString;
    readonly IGroupNameProvider _groupNameProvider;

    public AdoNetScheduledPublishRepository(IAdoNetConnectionString connectionString, IGroupNameProvider publishGroupNameProvider)
    {
      _connectionString = connectionString;
      _groupNameProvider = publishGroupNameProvider;
    }

    private IDbConnection OpenConnection()
    {
      IDbConnection connection = new SqlConnection(_connectionString.ConnectionString);
      connection.Open();
      return connection;
    }

    public void Clear()
    {
      using (IDbConnection connection = OpenConnection())
      {
        IDbCommand delete = CreateDeleteAllCommand(connection);
        delete.Parameter("GroupName").Value = _groupNameProvider.GroupName;
        delete.ExecuteNonQuery();
      }
    }

    public void Add(ScheduledPublish scheduled)
    {
      using (IDbConnection connection = OpenConnection())
      {
        foreach (EndpointAddress destination in scheduled.Addresses)
        {
          IDbCommand command = CreateInsertCommand(connection);
          command.Parameter("GroupName").Value = _groupNameProvider.GroupName;
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
        command.Parameter("GroupName").Value = _groupNameProvider.GroupName;
        using (IDataReader reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            DateTime publishAt = reader.GetDateTime(2);
            string returnAddress = reader.GetString(3);
            string messagePayload = reader.GetString(4);
            string sagaIds = reader.GetString(5);
            scheduled.Add(new ScheduledPublish(publishAt, MessagePayload.FromString(messagePayload), new[] { EndpointAddress.FromString(returnAddress) }, sagaIds.ToGuidArray()));
          }
          reader.Close();
        }
        IDbCommand delete = CreateDeleteCommand(connection);
        delete.Parameter("Now").Value = now;
        delete.Parameter("GroupName").Value = _groupNameProvider.GroupName;
        delete.ExecuteNonQuery();
        return scheduled;
      }
    }

    private static IDbCommand CreateInsertCommand(IDbConnection connection)
    {
      IDbCommand command = connection.CreateCommand();
      command.Connection = connection;
      command.CommandText = "INSERT INTO future_publish (CreatedAt, GroupName, PublishId, PublishAt, ReturnAddress, MessagePayload, SagaIds) VALUES (getutcdate(), @GroupName, @PublishId, @PublishAt, @ReturnAddress, @MessagePayload, @SagaIds)";
      command.CreateParameter("GroupName", DbType.String);
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
      command.CommandText = "SELECT Id, PublishId, PublishAt, ReturnAddress, MessagePayload, SagaIds FROM future_publish WHERE PublishAt < @Now AND GroupName = @GroupName";
      command.CreateParameter("GroupName", DbType.String);
      command.CreateParameter("Now", DbType.DateTime);
      return command;
    }

    private static IDbCommand CreateDeleteCommand(IDbConnection connection)
    {
      IDbCommand command = connection.CreateCommand();
      command.Connection = connection;
      command.CommandText = "DELETE FROM future_publish WHERE PublishAt < @Now AND GroupName = @GroupName";
      command.CreateParameter("GroupName", DbType.String);
      command.CreateParameter("Now", DbType.DateTime);
      return command;
    }

    private static IDbCommand CreateDeleteAllCommand(IDbConnection connection)
    {
      IDbCommand command = connection.CreateCommand();
      command.Connection = connection;
      command.CommandText = "DELETE FROM future_publish WHERE GroupName = @GroupName";
      command.CreateParameter("GroupName", DbType.String);
      return command;
    }
  }
  public static class MappingHelpers
  {
    public static string MakeString(this Guid[] guids)
    {
      return guids.Select(x => x.ToString()).Join(",");
    }

    public static Guid[] ToGuidArray(this string value)
    {
      if (String.IsNullOrEmpty(value))
      {
        return new Guid[0];
      }
      return value.Split(',').Select(x => new Guid(x)).ToArray();
    }

    public static string MakeString<T>(this T obj)
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