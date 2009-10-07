using System;
using System.Collections.Generic;
using System.Configuration;
using System.Xml.Serialization;
using System.Xml;
using System.Linq;

using Machine.Core.Utility;

namespace Machine.Mta.Configuration
{
  public class MessageBusEndpoint
  {
    string _name;
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

    public EndpointAddress ToEndpointAddress()
    {
      if (String.IsNullOrEmpty(_host))
      {
        return NameAndHostAddress.ForLocalQueue(_queue).ToAddress();
      }
      return NameAndHostAddress.ForRemoteQueue(_host, _queue).ToAddress();
    }
  }
  
  public class MessageForward
  {
    string _toQueue;
    string _messageType;

    [XmlAttribute]
    public string To
    {
      get { return _toQueue; }
      set { _toQueue = value; }
    }

    [XmlAttribute]
    public string Message
    {
      get { return _messageType; }
      set { _messageType = value; }
    }

    public Type MessageType
    {
      get
      {
        Type type = Type.GetType(_messageType);
        if (type == null)
        {
          throw new InvalidOperationException("No such message: " + _messageType);
        }
        return type;
      }
    }

    public void Apply(MessageBusConfigurationSection configuration, IMessageDestinations lookup)
    {
      IEnumerable<EndpointAddress> addresses = configuration.Lookup(this.To);
      if (String.IsNullOrEmpty(_messageType))
      {
        lookup.SendAllTo(addresses.ToArray());
      }
      else
      {
        lookup.SendMessageTypeTo(this.MessageType, addresses.ToArray());
      }
    }
  }
  
  [XmlRoot("messaging")]
  public class MessageBusConfigurationSection
  {
    private readonly List<MessageBusEndpoint> _endpoints = new List<MessageBusEndpoint>();
    private readonly List<MessageForward> _forwards = new List<MessageForward>();

    [XmlElement("endpoint")]
    public List<MessageBusEndpoint> Endpoints
    {
      get { return _endpoints; }
    }

    [XmlElement("forward")]
    public List<MessageForward> Forwards
    {
      get { return _forwards; }
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

    public void ApplyForwards(IMessageDestinations messageDestinations)
    {
      foreach (MessageForward forward in _forwards)
      {
        forward.Apply(this, messageDestinations);
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
