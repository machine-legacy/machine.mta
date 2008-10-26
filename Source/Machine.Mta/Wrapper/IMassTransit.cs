using System;

namespace Machine.Mta.Wrapper
{
  public interface IMassTransit : IDisposable
  {
    void Start();
    void Stop();
  }
}