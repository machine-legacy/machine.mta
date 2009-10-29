using System.Data;

namespace NServiceBus.Unicast.Subscriptions.AdoDotNet
{
  public interface IAdoNetSubscriptionSystem
  {
    IDbConnection OpenConnection();
    string TableName();
  }
}