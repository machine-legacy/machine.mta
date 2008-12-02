using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Machine.Mta.Sagas
{
  public class SagaException : Exception
  {
    public SagaException() { }
    public SagaException(string message) : base(message) { }
    public SagaException(string message, Exception innerException) : base(message, innerException) { }
    protected SagaException(SerializationInfo info, StreamingContext context) : base(info, context) { }
  }
  public class SagaStateRepositoryNotFoundException : SagaException
  {
    public SagaStateRepositoryNotFoundException() { }
    public SagaStateRepositoryNotFoundException(Type stateType) : this(stateType.ToString()) { }
    public SagaStateRepositoryNotFoundException(string message) : base(message) { }
    public SagaStateRepositoryNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    protected SagaStateRepositoryNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
  }
  public class SagaStateNotFoundException : SagaException
  {
    public SagaStateNotFoundException() { }
    public SagaStateNotFoundException(Guid sagaId) : this(sagaId.ToString()) { }
    public SagaStateNotFoundException(string message) : base(message) { }
    public SagaStateNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    protected SagaStateNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
  }
}
