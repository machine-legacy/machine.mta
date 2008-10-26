using System;

namespace Machine.Mta
{
  public interface IMtaUriFactory
  {
    Uri CreateUri(EndpointName name);
    Uri CreateUri(string name);
    Uri CreateUri(string address, string name);
    Uri CreateUri(Uri uri);
  }
}