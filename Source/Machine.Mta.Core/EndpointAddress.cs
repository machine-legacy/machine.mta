using System;

namespace Machine.Mta
{
  [Serializable]
  public class EndpointAddress
  {
    readonly string _address;

    public string Address
    {
      get { return _address; }
    }

    public static EndpointAddress Null1
    {
      get { return Null; }
    }

    protected EndpointAddress()
    {
    }

    protected EndpointAddress(string address)
    {
      _address = address;
    }

    public bool Equals(EndpointAddress other)
    {
      if (ReferenceEquals(null, other)) return false;
      if (ReferenceEquals(this, other)) return true;
      return Equals(other._address, _address);
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj)) return false;
      if (ReferenceEquals(this, obj)) return true;
      if (obj.GetType() != typeof (EndpointAddress)) return false;
      return Equals((EndpointAddress) obj);
    }

    public override Int32 GetHashCode()
    {
      return (_address != null ? _address.GetHashCode() : 0);
    }

    public static EndpointAddress FromString(string value)
    {
      return new EndpointAddress(value);
    }

    public static readonly EndpointAddress Null = new EndpointAddress();
  }

  public class NameAndHostAddress
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
    
    protected NameAndHostAddress()
    {
      _name = null;
    }

    protected NameAndHostAddress(string host, string name)
    {
      if (String.IsNullOrEmpty(host)) throw new ArgumentNullException("host");
      if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");
      _host = host;
      _name = name;
    }

    public override bool Equals(object obj)
    {
      var other = obj as NameAndHostAddress;
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

    public static NameAndHostAddress ForRemoteQueue(string address, string queue)
    {
      return new NameAndHostAddress(address, queue);
    }

    public static NameAndHostAddress ForLocalQueue(string queue)
    {
      return ForRemoteQueue("localhost", queue);
    }

    public static NameAndHostAddress FromString(string value)
    {
      string[] fields = value.Split('@');
      return ForRemoteQueue(fields[1], fields[0]);
    }

    public static NameAndHostAddress FromAddress(EndpointAddress address)
    {
      return FromString(address.Address);
    }

    public static readonly NameAndHostAddress Null = new NameAndHostAddress();
  }

  public static class AddressMappings
  {
    public static NameAndHostAddress ToNameAndHost(this EndpointAddress address)
    {
      return NameAndHostAddress.FromAddress(address);
    }
    
    public static EndpointAddress ToAddress(this NameAndHostAddress address)
    {
      return EndpointAddress.FromString(address.ToString());
    }
  }
}
