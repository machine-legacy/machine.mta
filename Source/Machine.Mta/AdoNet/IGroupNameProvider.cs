using System;

namespace Machine.Mta.AdoNet
{
  public interface IGroupNameProvider
  {
    string GroupName
    {
      get;
    }
  }
  public class MachineNameGroupNameProvider : IGroupNameProvider
  {
    public string GroupName
    {
      get { return Environment.MachineName.ToUpper(); }
    }
  }
  public class EmptyGroupNameProvider : IGroupNameProvider
  {
    public string GroupName
    {
      get { return String.Empty; }
    }
  }
}