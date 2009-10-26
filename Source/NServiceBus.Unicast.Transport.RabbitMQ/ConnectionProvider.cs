using System;
using System.Collections.Generic;
using System.Transactions;

using RabbitMQ.Client;

namespace NServiceBus.Unicast.Transport.RabbitMQ
{
  public class ConnectionProvider
  {
    readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(ConnectionProvider));
    readonly ConnectionFactory _connectionFactory;
    [ThreadStatic]
    static Dictionary<string, OpenedSession> _state;

    public ConnectionProvider()
    {
      _connectionFactory = new ConnectionFactory();
    }

    public OpenedSession Open(string protocolName, string brokerAddress, bool transactional)
    {
      if (!transactional)
      {
        var opened = OpenNew(protocolName, brokerAddress);
        opened.Disposed += (sender, e) => {
          _log.Debug("Closing " + brokerAddress);
        };
        return opened;
      }
      return OpenTransactional(protocolName, brokerAddress);
    }

    OpenedSession OpenTransactional(string protocolName, string brokerAddress)
    {
      if (_state == null)
      {
        _state = new Dictionary<string, OpenedSession>();
      }
      if (_state.ContainsKey(brokerAddress))
      {
        var existing = _state[brokerAddress].AddRef();
        if (existing.IsActive)
        {
          return existing;
        }
        _state.Remove(brokerAddress);
      }
      var opened = _state[brokerAddress] = OpenNew(protocolName, brokerAddress);
      opened.Disposed += (sender, e) => {
        _log.Debug("Closing " + brokerAddress);
        if (_state != null)
        {
          _state.Remove(brokerAddress);
        }
      };
      /*
      if (Transaction.Current != null)
      {
        Transaction.Current.EnlistVolatile(new RabbitMqEnlistment(opened), EnlistmentOptions.None);
        opened.AddRef();
      }
      */
      return opened.AddRef();
    }

    OpenedSession OpenNew(string protocolName, string brokerAddress)
    {
      var protocol = GetProtocol(protocolName);
      var connection = _connectionFactory.CreateConnection(protocol, brokerAddress);
      var model = connection.CreateModel();
      _log.Debug("Opening " + brokerAddress + " using " + protocol.ApiName);
      return new OpenedSession(connection, model);
    }

    static IProtocol GetProtocol(string protocolName)
    {
      if (String.IsNullOrEmpty(protocolName))
      {
        return Protocols.FromConfiguration();
      }
      return Protocols.SafeLookup(protocolName);
    }
  }
}