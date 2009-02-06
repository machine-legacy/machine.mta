using System;
using System.Collections.Generic;
using System.Threading;

namespace Machine.Mta.Internal
{
  public class RequestReplyBuilder : IRequestReplyBuilder
  {
    private readonly TransportMessage _request;
    private readonly AsyncCallbackMap _asyncCallbackMap;

    public RequestReplyBuilder(TransportMessage request, AsyncCallbackMap asyncCallbackMap)
    {
      _request = request;
      _asyncCallbackMap = asyncCallbackMap;
    }

    public void OnReply(AsyncCallback callback, object state)
    {
      _asyncCallbackMap.Add(_request.Id, callback, state);
    }

    public void OnReply(AsyncCallback callback)
    {
      OnReply(callback, null);
    }
  }

  public class Reply
  {
    private readonly object _state;
    private readonly IMessage[] _messages;

    public object State
    {
      get { return _state; }
    }

    public IMessage[] Messages
    {
      get { return _messages; }
    }

    public Reply(object state, IMessage[] messages)
    {
      _state = state;
      _messages = messages;
    }
  }

  public class MessageBusAsyncResult : IAsyncResult
  {
    private readonly AsyncCallback _callback;
    private readonly object _state;
    private readonly ManualResetEvent _waitHandle;
    private volatile bool _completed;
    private Reply _reply;

    public object AsyncState
    {
      get { return _reply; }
    }

    public WaitHandle AsyncWaitHandle
    {
      get { return _waitHandle; }
    }

    public bool CompletedSynchronously
    {
      get { return false; }
    }

    public bool IsCompleted
    {
      get { return _completed; }
    }

    public MessageBusAsyncResult(AsyncCallback callback, object state)
    {
      _callback = callback;
      _state = state;
      _waitHandle = new ManualResetEvent(false);
    }

    public void Complete(params IMessage[] messages)
    {
      _reply = new Reply(_state, messages);
      _completed = true;
      _waitHandle.Set();
      if (_callback != null)
      {
        _callback(this);
      }
    }
  }

  public class AsyncCallbackMap
  {
    private readonly Dictionary<Guid, MessageBusAsyncResult> _map = new Dictionary<Guid, MessageBusAsyncResult>();

    public void Add(Guid id, AsyncCallback callback, object state)
    {
      lock (_map)
      {
        _map[id] = new MessageBusAsyncResult(callback, state);
      }
    }

    public void InvokeAndRemove(Guid id, IMessage[] messages)
    {
      MessageBusAsyncResult ar;
      lock (_map)
      {
        if (!_map.TryGetValue(id, out ar))
        {
          return;
        }
        _map.Remove(id);
      }
      ar.Complete(messages);
    }
  }
}
