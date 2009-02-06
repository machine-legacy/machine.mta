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
        return EndpointAddress.ForLocalQueue(_queue);
      }
      return EndpointAddress.ForRemoteQueue(_host, _queue);
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
      EndpointAddress endpointAddress = configuration.Lookup(this.To);
      if (String.IsNullOrEmpty(_messageType))
      {
        lookup.SendAllTo(endpointAddress);
      }
      else
      {
        lookup.SendMessageTypeTo(this.MessageType, endpointAddress);
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

    public EndpointAddress Lookup(string name)
    {
      foreach (MessageBusEndpoint endpoint in _endpoints)
      {
        if (endpoint.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
        {
          return endpoint.ToEndpointAddress();
        }
      }
      throw new KeyNotFoundException("No endpoint configured: " + name);
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
