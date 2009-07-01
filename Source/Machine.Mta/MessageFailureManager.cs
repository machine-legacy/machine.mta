using System;
using System.Collections.Generic;
using System.Threading;
using Machine.Container;
using Machine.Core.Utility;
using Machine.Mta.Errors;
using Machine.Mta.Timing;

namespace Machine.Mta
{
  public class FailureConfiguration
  {
    private readonly Int32 _maximumRetries;

    public Int32 MaximumRetries
    {
      get { return _maximumRetries; }
    }

    public FailureConfiguration(Int32 maximumRetries)
    {
      if (maximumRetries < 1) throw new ArgumentException("maximumRetries");
      _maximumRetries = maximumRetries;
    }
  }

  public class MessageFailureManager : IMessageFailureManager
  {
    readonly Dictionary<string, List<Exception>> _errors = new Dictionary<string, List<Exception>>();
    readonly ReaderWriterLock _lock = new ReaderWriterLock();
    readonly FailureConfiguration _configuration;

    public MessageFailureManager(FailureConfiguration configuration)
    {
      _configuration = configuration;
    }

    public virtual void RecordFailure(EndpointAddress address, TransportMessage transportMessage, Exception error)
    {
      if (transportMessage != null)
      {
        using (RWLock.AsWriter(_lock))
        {
          string id = transportMessage.Id;
          if (!_errors.ContainsKey(id))
          {
            _errors[id] = new List<Exception>();
          }
          _errors[id].Add(error);
        }
      }
      if (Failure != null)
      {
        Failure(address, transportMessage, error);
      }
    }

    public void RecordSuccess(TransportMessage transportMessage)
    {
      string id = transportMessage.Id;
      using (RWLock.AsReader(_lock))
      {
        if (RWLock.UpgradeToWriterIf(_lock, () => _errors.ContainsKey(id)))
        {
          _errors.Remove(id);
        }
      }
    }

    public virtual bool IsPoison(TransportMessage transportMessage)
    {
      string id = transportMessage.Id;
      using (RWLock.AsReader(_lock))
      {
        if (RWLock.UpgradeToWriterIf(_lock, () => _errors.ContainsKey(id)))
        {
          if (_errors[id].Count >= _configuration.MaximumRetries)
          {
            _errors.Remove(id);
            return true;
          }
        }
      }
      return false;
    }

    public static Action<EndpointAddress, TransportMessage, Exception> Failure;
  }

  public interface IMessageFailureManager
  {
    void RecordFailure(EndpointAddress address, TransportMessage transportMessage, Exception error);
    void RecordSuccess(TransportMessage transportMessage);
    bool IsPoison(TransportMessage transportMessage);
  }
  /*
  public class PublishErrorMessages : IStartable
  {
    readonly IMessageBus _bus;
    readonly IMessageFactory _factory;

    public PublishErrorMessages(IMessageBus bus, IMessageFactory factory)
    {
      _bus = bus;
      _factory = factory;
    }

    public void RecordFailure(EndpointAddress address, TransportMessage transportMessage, Exception error)
    {
      _bus.Publish(_factory.ErrorMessage(ServerClock.Now(), error, address, transportMessage));
    }

    public void Start()
    {
      MessageFailureManager.Failure = RecordFailure;
    }
  }
  */
}