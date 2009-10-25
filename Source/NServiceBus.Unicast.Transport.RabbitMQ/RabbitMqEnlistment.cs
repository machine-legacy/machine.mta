using System;
using System.Transactions;
using RabbitMQ.Client;

namespace NServiceBus.Unicast.Transport.RabbitMQ
{
  public class RabbitMqEnlistment : IEnlistmentNotification
  {
    readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(RabbitMqEnlistment));
    readonly OpenedSession _openedSession;

    public RabbitMqEnlistment(OpenedSession openedSession)
    {
      _openedSession = openedSession;
    }

    public void Prepare(PreparingEnlistment preparingEnlistment)
    {
      _log.Debug("Prepared");
      preparingEnlistment.Prepared();
    }

    public void Commit(Enlistment enlistment)
    {
      _log.Debug("Commit");
      _openedSession.Dispose();
      enlistment.Done();
    }

    public void Rollback(Enlistment enlistment)
    {
      _log.Debug("Rollback");
      _openedSession.Dispose();
      enlistment.Done();
    }

    public void InDoubt(Enlistment enlistment)
    {
      _log.Debug("Doubt");
    }
  }

  public class OpenedSession : IDisposable
  {
    readonly IConnection _connection;
    readonly IModel _model;
    Int32 _refs;
    public event EventHandler Disposed;

    public bool IsActive
    {
      get { return _refs > 0; }
    }

    public OpenedSession(IConnection connection, IModel model)
    {
      _connection = connection;
      _model = model;
    }

    public OpenedSession AddRef()
    {
      _refs++;
      return this;
    }

    public void Dispose()
    {
      if (--_refs == 0)
      {
        _model.Dispose();
        _connection.Dispose();
        Disposed(this, EventArgs.Empty);
      }
    }

    public IModel Model()
    {
      return _model;
    }
  }
}
