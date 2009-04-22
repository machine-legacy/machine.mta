using System;
using System.Collections.Generic;
using System.Threading;

namespace Machine.Mta
{
  public class RequestReplyBuilder : IRequestReplyBuilder
  {
    readonly TransportMessage _request;
    readonly Action<TransportMessage> _sendRequest;
    readonly AsyncCallbackMap _asyncCallbackMap;

    public RequestReplyBuilder(TransportMessage request, Action<TransportMessage> sendRequest, AsyncCallbackMap asyncCallbackMap)
    {
      _request = request;
      _sendRequest = sendRequest;
      _asyncCallbackMap = asyncCallbackMap;
    }

    public void OnReply(AsyncCallback callback)
    {
      OnReply(callback, null);
    }

    public void OnReply(AsyncCallback callback, object state)
    {
      _asyncCallbackMap.Add(_request.CorrelationId, callback, state);
      _sendRequest(_request);
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
      if (_callback != null)
      {
        _callback(this);
      }
      _waitHandle.Set();
    }
  }

  public class AsyncCallbackMap
  {
    readonly Dictionary<string, MessageBusAsyncResult> _map = new Dictionary<string, MessageBusAsyncResult>();

    public void Add(string id, AsyncCallback callback, object state)
    {
      lock (_map)
      {
        _map[id] = new MessageBusAsyncResult(callback, state);
      }
    }

    public void InvokeAndRemove(string id, IMessage[] messages)
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
