using System;

namespace Machine.Mta.Internal
{
  public class MsMqUriFactory : IMtaUriFactory
  {
    public Uri CreateUri(EndpointName name)
    {
      return CreateUri(name.Address, name.Name);
    }

    public Uri CreateUri(string name)
    {
      return CreateUri("localhost", name);
    }

    public Uri CreateUri(Uri uri)
    {
      return new Uri("msmq://" + uri.Host + uri.AbsolutePath);
    }

    public Uri CreateUri(string address, string name)
    {
      return new Uri("msmq://" + address + "/" + name);
    }
  }
}