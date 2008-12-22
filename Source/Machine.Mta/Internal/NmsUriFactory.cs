using System;

namespace Machine.Mta.Internal
{
  public class NmsUriFactory : IMtaUriFactory
  {
    public Uri CreateUri(EndpointName name)
    {
      return CreateUri(name.Address, name.Name);
    }

    public Uri CreateUri(string name)
    {
      return CreateUri("127.0.0.1", name);
    }

    public Uri CreateUri(Uri uri)
    {
      return new Uri("activemq://" + uri.Host + ":61616" + uri.AbsolutePath);
    }

    public Uri CreateUri(string address, string name)
    {
      return new Uri("activemq://" + address + ":61616/" + name);
    }
  }
}