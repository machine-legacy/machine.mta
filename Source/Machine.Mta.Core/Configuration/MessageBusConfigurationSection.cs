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
    string _host;
    string _queue;

    [XmlAttribute]
    public string Name
    {
      get { return _name; }
      set { _name = value; }
    }

    [XmlAttribute]
    public string Host
    {
      get { return _host; }
      set { _host = value; }
    }

    [XmlAttribute]
    public string Queue
    {
      get { return _queue; }
      set { _queue = value; }
    }

    [XmlAttribute]
    public string Address
    {
      get { return _address; }
      set { _address = value; }
    }

    public EndpointAddress ToEndpointAddress()
    {
      if (!String.IsNullOrEmpty(_address))
      {
        return EndpointAddress.FromString(_address);
      }
      if (String.IsNullOrEmpty(_host))
      {
        return NameAndHostAddress.ForLocalQueue(_queue).ToAddress();
      }
      return NameAndHostAddress.ForRemoteQueue(_host, _queue).ToAddress();
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

    public IEnumerable<EndpointAddress> Lookup(string name)
    {
      foreach (MessageBusEndpoint endpoint in _endpoints)
      {
        if (endpoint.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
        {
          yield return endpoint.ToEndpointAddress();
        }
      }
    }

    public static MessageBusConfigurationSection Read(string name)
    {
      return (MessageBusConfigurationSection)ConfigurationManager.GetSection(name);
    }

    private static MessageBusConfigurationSection _configuration;

    public static void UseConfiguration(MessageBusConfigurationSection configuration)
    {
      _configuration = configuration;
    }

    public static MessageBusConfigurationSection Read()
    {
      if (_configuration != null)
      {
        return _configuration;
      }
      return _configuration = Read("messaging");
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
