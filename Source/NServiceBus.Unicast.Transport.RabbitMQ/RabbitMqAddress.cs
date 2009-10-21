using System;
using System.Collections.Generic;

namespace NServiceBus.Unicast.Transport.RabbitMQ
{
  [Serializable]
  public class RabbitMqAddress
  {
    readonly string _broker;
    readonly string _exchange;
    readonly string _routingKey;

    public string Broker
    {
      get { return _broker; }
    }

    public string Exchange
    {
      get { return _exchange; }
    }

    public string RoutingKey
    {
      get { return _routingKey; }
    }

    public RabbitMqAddress(string broker, string exchange, string routingKey)
    {
      _broker = broker;
      _exchange = exchange;
      _routingKey = routingKey;
    }

    public bool Equals(RabbitMqAddress other)
    {
      if (ReferenceEquals(null, other)) return false;
      if (ReferenceEquals(this, other)) return true;
      return Equals(other._broker, _broker) && Equals(other._exchange, _exchange) && Equals(other._routingKey, _routingKey);
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj)) return false;
      if (ReferenceEquals(this, obj)) return true;
      if (obj.GetType() != typeof (RabbitMqAddress)) return false;
      return Equals((RabbitMqAddress) obj);
    }

    public override Int32 GetHashCode()
    {
      var hashCode = _broker.GetHashCode();
      hashCode = (hashCode * 397) ^ _exchange.GetHashCode();
      hashCode = (hashCode * 397) ^ _routingKey.GetHashCode();
      return hashCode;
    }

    public override string ToString()
    {
      return "rmq://" + _broker + "//" + _exchange + "/" + _routingKey;
    }

    public static RabbitMqAddress FromString(string value)
    {
      var fields = value.Split('/');
      if (fields.Length != 6)
        throw new FormatException("RabbitMQ publication address was badly formatted: " + value);
      return new RabbitMqAddress(fields[2], fields[4], fields[5]);
    }
  }
}
