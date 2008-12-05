using System;

using Newtonsoft.Json;

namespace Machine.Mta
{
  public class EndpointNameJsonConverter : JsonConverter
  {
    public override bool CanConvert(Type objectType)
    {
      return typeof(EndpointName).IsAssignableFrom(objectType);
    }

    public override object ReadJson(JsonReader reader, Type objectType)
    {
      return EndpointName.FromString(reader.Value.ToString());
    }

    public override void WriteJson(JsonWriter writer, object value)
    {
      writer.WriteValue(value.ToString());
    }
  }
  [Serializable]
  public class EndpointName
  {
    private readonly string _address;
    private readonly string _name;

    public string Address
    {
      get { return _address; }
    }

    public string Name
    {
      get { return _name; }
    }

    protected EndpointName()
    {
      _name = null;
    }

    protected EndpointName(string address, string name)
    {
      if (String.IsNullOrEmpty(address)) throw new ArgumentNullException("address");
      if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");
      _address = address;
      _name = name;
    }

    public override bool Equals(object obj)
    {
      EndpointName other = obj as EndpointName;
      if (other != null)
      {
        return other.Address.Equals(this.Address) && other.Name.Equals(this.Name);
      }
      return false;
    }

    public override Int32 GetHashCode()
    {
      return _address.GetHashCode() ^ _name.GetHashCode();
    }

    public override string ToString()
    {
      return _name + "@" + _address;
    }

    public static readonly EndpointName Null = new EndpointName();

    public static EndpointName FromString(string value)
    {
      string[] fields = value.Split('@');
      return ForRemoteQueue(fields[1], fields[0]);
    }

    public static EndpointName ForRemoteQueue(string address, string queue)
    {
      return new EndpointName(address, queue);
    }

    public static EndpointName ForLocalQueue(string queue)
    {
      return ForRemoteQueue("localhost", queue);
    }
  }
}
