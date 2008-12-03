using System;
using System.Data;

namespace Machine.Mta.AdoNet
{
  public static class DatabaseHelpers
  {
    public static void CreateParameter(this IDbCommand command, string name, DbType dbType)
    {
      IDbDataParameter parameter = command.CreateParameter();
      parameter.ParameterName = name;
      parameter.DbType = dbType;
      command.Parameters.Add(parameter);
    }

    public static IDbDataParameter Parameter(this IDbCommand command, string name)
    {
      return ((IDbDataParameter)command.Parameters[name]);
    }
  }
}