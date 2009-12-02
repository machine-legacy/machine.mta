using System.Data;
using System.Data.SqlClient;

namespace NServiceBus.Unicast.Subscriptions.AdoDotNet
{
  public class SqlServerSubscriptionSystem : IAdoNetSubscriptionSystem
  {
    readonly string _connectionString;

    public SqlServerSubscriptionSystem(string connectionString)
    {
      _connectionString = connectionString;
    }

    public IDbConnection OpenConnection()
    {
      var connection = new SqlConnection(_connectionString);
      connection.Open();
      return connection;
    }

    public string TableName()
    {
      return "subscriptions";
    }
  }
}