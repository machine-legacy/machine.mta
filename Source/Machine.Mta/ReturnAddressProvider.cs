using System;

namespace Machine.Mta
{
  public class ReturnAddressProvider
  {
    public virtual EndpointAddress GetReturnAddress(EndpointAddress listeningOn)
    {
      if (listeningOn.IsLocal)
      {
        return EndpointAddress.ForRemoteQueue(Environment.MachineName, listeningOn.Name);
      }
      return listeningOn;
    }
  }
}