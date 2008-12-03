using System;

namespace Machine.Mta.AdoNet
{
  public interface IAdoNetSagaRepositoryConfiguration : IAdoNetConnectionString
  {
    string TableName { get; }
  }
}