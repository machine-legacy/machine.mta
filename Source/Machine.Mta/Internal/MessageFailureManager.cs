using System;
using System.Collections.Generic;

namespace Machine.Mta.Internal
{
  public class MessageFailureManager
  {
    readonly Dictionary<Guid, List<Exception>> _errors = new Dictionary<Guid, List<Exception>>();
    readonly object _lock = new object();

    public void RecordFailure(Guid id, Exception error)
    {
      lock (_lock)
      {
        if (!_errors.ContainsKey(id))
        {
          _errors[id] = new List<Exception>();
        }
        _errors[id].Add(error);
      }
    }

    public bool SendToPoisonQueue(Guid id)
    {
      lock (_lock)
      {
        bool hasErrors = _errors.ContainsKey(id);
        if (hasErrors)
        {
          _errors.Remove(id);
        }
        return hasErrors;
      }
    }
  }
}