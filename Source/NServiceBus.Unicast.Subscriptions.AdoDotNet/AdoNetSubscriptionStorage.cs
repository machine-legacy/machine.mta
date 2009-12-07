using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace NServiceBus.Unicast.Subscriptions.AdoDotNet
{
  public class AdoNetSubscriptionStorage : ISubscriptionStorage, IDisposable
  {
    readonly static log4net.ILog _log = log4net.LogManager.GetLogger(typeof(AdoNetSubscriptionStorage));
    readonly IAdoNetSubscriptionSystem _subscriptionSystem;

    public AdoNetSubscriptionStorage(IAdoNetSubscriptionSystem subscriptionSystem)
    {
      _subscriptionSystem = subscriptionSystem;
    }

    public void Subscribe(string address, IEnumerable<string> messageTypes)
    {
      _log.Info("Subscribing " + address + " to " + String.Join(", ", messageTypes.ToArray()));
      using (var connection = CreateConnection())
      {
        foreach (var messageType in messageTypes)
        {
          if (!HasSubscription(connection, address, messageType))
          {
            InsertSubscription(connection, address, messageType);
          }
        }
      }
    }

    public void Unsubscribe(string address, IEnumerable<string> messageTypes)
    {
      _log.Info("Unsubscribing " + address + " to " + String.Join(", ", messageTypes.ToArray()));
      using (var connection = CreateConnection())
      {
        foreach (var messageType in messageTypes)
        {
          DeleteSubscription(connection, address, messageType);
        }
      }
    }

    public IEnumerable<string> GetSubscribersForMessage(IEnumerable<string> messageTypes)
    {
      var subscribers = new List<string>();
      using (var connection = CreateConnection())
      {
        foreach (var messageType in messageTypes)
        {
          using (var command = CreateSelectByMessageTypeCommand(connection))
          {
            command.Parameter("MessageType").Value = messageType;
            using (var reader = command.ExecuteReader())
            {
              while (reader.Read())
              {
                subscribers.Add(reader.GetString(0));
              }
              reader.Close();
            }
          }
        }
      }
      return subscribers;
    }

    public void Init()
    {
    }

    public void Dispose()
    {
    }

    IDbConnection CreateConnection()
    {
      return _subscriptionSystem.OpenConnection();
    }

    static IDbCommand CreateCommand(IDbConnection connection)
    {
      var command = connection.CreateCommand();
      command.Connection = connection;
      return command;
    }

    string TableName()
    {
      return _subscriptionSystem.TableName();
    }

    bool HasSubscription(IDbConnection connection, string address, string messageType)
    {
      using (var command = CreateSelectCommand(connection))
      {
        command.Parameter("Address").Value = address;
        command.Parameter("MessageType").Value = messageType;
        var value = command.ExecuteScalar();
        if (value == null || (Int32)value == 0)
        {
          return false;
        }
        return true;
      }
    }

    void InsertSubscription(IDbConnection connection, string address, string messageType)
    {
      using (var command = CreateInsertCommand(connection))
      {
        command.Parameter("Address").Value = address;
        command.Parameter("MessageType").Value = messageType;
        if (command.ExecuteNonQuery() != 1)
        {
          throw new InvalidOperationException("Failed to insert Subscription for: " + address + " to " + messageType);
        }
      }
    }

    void DeleteSubscription(IDbConnection connection, string address, string messageType)
    {
      using (var command = CreateDeleteCommand(connection))
      {
        command.Parameter("Address").Value = address;
        command.Parameter("MessageType").Value = messageType;
        command.ExecuteNonQuery();
      }
    }

    IDbCommand CreateInsertCommand(IDbConnection connection)
    {
      var command = CreateCommand(connection);
      command.CommandText = "INSERT INTO " + TableName() + " (Address, MessageType) VALUES (@Address, @MessageType)";
      command.CreateParameter("Address", DbType.String);
      command.CreateParameter("MessageType", DbType.String);
      return command;
    }

    IDbCommand CreateSelectByMessageTypeCommand(IDbConnection connection)
    {
      var command = CreateCommand(connection);
      command.CommandText = "SELECT Address, MessageType FROM " + TableName() + " WHERE MessageType = @MessageType";
      command.CreateParameter("MessageType", DbType.String);
      return command;
    }

    IDbCommand CreateSelectCommand(IDbConnection connection)
    {
      var command = CreateCommand(connection);
      command.CommandText = "SELECT COUNT(*) FROM " + TableName() + " WHERE Address = @Address AND MessageType = @MessageType";
      command.CreateParameter("Address", DbType.String);
      command.CreateParameter("MessageType", DbType.String);
      return command;
    }

    IDbCommand CreateDeleteCommand(IDbConnection connection)
    {
      var command = CreateCommand(connection);
      command.CommandText = "DELETE FROM " + TableName() + " WHERE Address = @Address AND MessageType = @MessageType";
      command.CreateParameter("Address", DbType.String);
      command.CreateParameter("MessageType", DbType.String);
      return command;
    }
  }

  public static class AdoNetHelpers
  {
    public static void CreateParameter(this IDbCommand command, string name, DbType type)
    {
      var parameter = command.CreateParameter();
      parameter.DbType = type;
      parameter.ParameterName = name;
      command.Parameters.Add(parameter);
    }

    public static IDbDataParameter Parameter(this IDbCommand command, string name)
    {
      return ((IDbDataParameter)command.Parameters[name]);
    }
  }
}
