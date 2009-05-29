using System;

using Newtonsoft.Json;

namespace Machine.Mta
{
  public class EndpointAddressJsonConverter : JsonConverter
  {
    public override bool CanConvert(Type objectType)
    {
      return typeof(EndpointAddress).IsAssignableFrom(objectType);
    }

    public override object ReadJson(JsonReader reader, Type objectType)
    {
      return EndpointAddress.FromString(reader.Value.ToString());
    }

    public override void WriteJson(JsonWriter writer, object value)
    {
      writer.WriteValue(value.ToString());
    }
  }
  [Serializable]
  public class EndpointAddress
  {
    private readonly string _host;
    private readonly string _name;

    public string Host
    {
      get { return _host; }
    }

    public string Name
    {
      get { return _name; }
    }

    public bool IsLocal
    {
      get
      {
        foreach (var hostValue in new[] { "localhost", Environment.MachineName })
        {
          if (hostValue.Equals(_host, StringComparison.InvariantCultureIgnoreCase))
          {
            return true;
          }
        }
        return false;
      }
    }

    protected EndpointAddress()
    {
      _name = null;
    }

    protected EndpointAddress(string host, string name)
    {
      if (String.IsNullOrEmpty(host)) throw new ArgumentNullException("host");
      if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");
      _host = host;
      _name = name;
    }

    public override bool Equals(object obj)
    {
      EndpointAddress other = obj as EndpointAddress;
      if (other != null)
      {
        return other.Host.Equals(this.Host) && other.Name.Equals(this.Name);
      }
      return false;
    }

    public override Int32 GetHashCode()
    {
      return _host.GetHashCode() ^ _name.GetHashCode();
    }

    public override string ToString()
    {
      return _name + "@" + _host;
    }

    public static readonly EndpointAddress Null = new EndpointAddress();

    public static EndpointAddress FromString(string value)
    {
      string[] fields = value.Split('@');
      return ForRemoteQueue(fields[1], fields[0]);
    }

    public static EndpointAddress ForRemoteQueue(string address, string queue)
    {
      return new EndpointAddress(address, queue);
    }

    public static EndpointAddress ForLocalQueue(string queue)
    {
      return ForRemoteQueue("localhost", queue);
    }
  }
}
