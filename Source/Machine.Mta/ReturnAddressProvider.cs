using System;

namespace Machine.Mta
{
  public class ReturnAddressProvider
  {
    public virtual EndpointAddress GetReturnAddress(EndpointAddress listeningOn)
    {
      var nameAndHostAddress = NameAndHostAddress.FromAddress(listeningOn);
      if (nameAndHostAddress.IsLocal)
      {
        return NameAndHostAddress.ForRemoteQueue(Environment.MachineName, nameAndHostAddress.Name).ToAddress();
      }
      return listeningOn;
    }
  }
}