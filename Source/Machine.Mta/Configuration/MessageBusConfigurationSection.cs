using System;
using System.Collections.Generic;
using System.Configuration;
using System.Xml.Serialization;
using System.Xml;

using Machine.Core.Utility;

namespace Machine.Mta.Configuration
{
  public class MessageBusEndpoint
  {
    string _name;
    string _address;
    string _queue;

    [XmlAttribute]
    public string Name
    {
      get { return _name; }
      set { _name = value; }
    }

    [XmlAttribute]
    public string Address
    {
      get { return _address; }
      set { _address = value; }
    }

    [XmlAttribute]
    public string Queue
    {
      get { return _queue; }
      set { _queue = value; }
    }

    public EndpointName ToEndpointName()
    {
      if (String.IsNullOrEmpty(_address))
      {
        return EndpointName.ForLocalQueue(_queue);
      }
      return EndpointName.ForRemoteQueue(_address, _queue);
    }
  }
  [XmlRoot("messaging")]
  public class MessageBusConfigurationSection
  {
    private readonly List<MessageBusEndpoint> _endpoints = new List<MessageBusEndpoint>();

    [XmlElement("endpoint")]
    public List<MessageBusEndpoint> Endpoints
    {
      get { return _endpoints; }
    }

    public EndpointName Lookup(string name)
    {
      foreach (MessageBusEndpoint endpoint in _endpoints)
      {
        if (endpoint.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
        {
          return endpoint.ToEndpointName();
        }
      }
      throw new KeyNotFoundException("No endpoint configured: " + name);
    }

    public static MessageBusConfigurationSection Read(string name)
    {
      return (MessageBusConfigurationSection)ConfigurationManager.GetSection(name);
    }

    public static MessageBusConfigurationSection Read()
    {
      return Read("messaging");
    }
  }
  public class MessageBusConfigurationSectionHandler : GenericConfigurationSectionHandler<MessageBusConfigurationSection>
  {
  }
  public abstract class GenericConfigurationSectionHandler<TCfg> : IConfigurationSectionHandler where TCfg : class
  {
    public object Create(object parent, object configContext, XmlNode section)
    {
      TCfg cfg = XmlSerializationHelper.DeserializeString<TCfg>(section.OuterXml);
      if (cfg == null)
      {
        throw new FormatException("Error reading configuration section: " + section);
      }
      return cfg;
    }
  }
}
