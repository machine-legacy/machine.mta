using System;

namespace Machine.Mta
{
  public class ReturnAddressProvider
  {
    public virtual EndpointName GetReturnAddress(EndpointName listeningOn)
    {
      if (listeningOn.IsLocal)
      {
        return EndpointName.ForRemoteQueue(Environment.MachineName, listeningOn.Name);
      }
      return listeningOn;
    }
  }
}