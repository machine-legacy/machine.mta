using System;

namespace Machine.Mta
{
  public interface IMassTransit : IDisposable
  {
    void Start();
    void Stop();
  }
}