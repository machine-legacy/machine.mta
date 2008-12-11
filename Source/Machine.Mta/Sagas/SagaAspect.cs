using System;
using System.Collections.Generic;
using System.Linq;

using Machine.Container.Services;
using Machine.Mta.Minimalistic;

namespace Machine.Mta.Sagas
{
  public class SagaAspect : IMessageAspect
  {
    static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(SagaAspect));
    readonly IMachineContainer _container;

    public SagaAspect(IMachineContainer container)
    {
      _container = container;
    }

    public void Continue(HandlerInvocation invocation)
    {
      if (!invocation.IsForSagaHandler())
      {
        invocation.Continue();
        return;
      }
      ISagaHandler handler = invocation.SagaHandler();
      ISagaStateRepository<ISagaState> repository = GetRepositoryFor(handler.SagaStateType);
      Guid sagaId = invocation.RetrieveSagaId();
      if (sagaId != Guid.Empty)
      {
        _log.Info("Retrieving: " + sagaId);
        handler.State = repository.FindSagaState(sagaId);
      }
      else
      {
        _log.Info("Non-Saga Message: " + handler);
        handler.State = null;
      }
      invocation.Continue();
      SaveOrDeleteState(handler, repository);
    }

    private static void SaveOrDeleteState(ISagaHandler handler, ISagaStateRepository<ISagaState> repository)
    {
      if (handler.State == null)
      {
        return;
      }
      if (handler.State.IsSagaComplete)
      {
        _log.Info("Deleting: " + handler.State.SagaId);
        repository.Delete(handler.State);
      }
      else
      {
        _log.Info("Saving: " + handler.State.SagaId);
        repository.Save(handler.State);
      }
    }

    private ISagaStateRepository<ISagaState> GetRepositoryFor(Type sagaStateType)
    {
      object repository = _container.Resolve.All(type => 
        type.IsSortOfContravariantWith(typeof(ISagaStateRepository<>).MakeGenericType(sagaStateType))).FirstOrDefault();
      if (repository == null)
      {
        throw new SagaStateRepositoryNotFoundException(sagaStateType);
      }
      return Invokers.CreateForRepository(sagaStateType, repository);
    }
  }

  public static class SagaHelpers
  {
    public static bool IsForSagaMessage(this HandlerInvocation invocation)
    {
      return invocation.MessageType.IsSagaMessage();
    }

    public static bool IsForSagaHandler(this HandlerInvocation invocation)
    {
      return invocation.HandlerType.IsSagaHandler();
    }
    
    public static Guid RetrieveSagaId(this HandlerInvocation invocation)
    {
      if (invocation.IsForSagaMessage())
      {
        return invocation.SagaMessage().SagaId;
      }
      if (CurrentMessageContext.Current != null)
      {
        TransportMessage transportMessage = CurrentMessageContext.Current.TransportMessage;
        if (transportMessage != null)
        {
          return transportMessage.SagaId;
        }
      }
      return Guid.Empty;
    }
    
    public static ISagaMessage SagaMessage(this HandlerInvocation invocation)
    {
      return invocation.Message as ISagaMessage;
    }

    public static ISagaHandler SagaHandler(this HandlerInvocation invocation)
    {
      return invocation.Handler as ISagaHandler;
    }

    private static bool IsSagaMessage(this Type messageType)
    {
      return typeof(ISagaMessage).IsAssignableFrom(messageType);
    }

    private static bool IsSagaHandler(this Type handlerType)
    {
      return typeof(ISagaHandler).IsAssignableFrom(handlerType);
    }
  }
}
