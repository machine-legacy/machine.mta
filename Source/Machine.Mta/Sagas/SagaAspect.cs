using System;
using System.Collections.Generic;
using System.Linq;

using Machine.Container.Services;
using Machine.Mta.Internal;

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
      if (sagaId == Guid.Empty)
      {
        if (!invocation.IsStartedBy())
        {
          invocation.HandlerLogger.Warn("Ignoring");
          return;
        }
        invocation.HandlerLogger.Info("Starting");
        handler.State = null;
      }
      else
      {
        invocation.HandlerLogger.Info("Retrieving: " + sagaId);
        ISagaState state = repository.FindSagaState(sagaId);
        if (state == null)
        {
          if (!invocation.IsStartedBy())
          {
            invocation.HandlerLogger.Warn("No state:: " + sagaId);
            return;
          }
          invocation.HandlerLogger.Info("Starting (No State): " + sagaId);
        }
        handler.State = state;
      }
      using (CurrentSagaContext.Open(sagaId))
      {
        invocation.Continue();
      }
      SaveOrDeleteState(invocation, handler, repository);
    }

    private static void SaveOrDeleteState(HandlerInvocation invocation, ISagaHandler handler, ISagaStateRepository<ISagaState> repository)
    {
      if (handler.State == null)
      {
        return;
      }
      if (handler.State.IsSagaComplete)
      {
        handler.Complete();
        invocation.HandlerLogger.Info("Deleting: " + handler.State.SagaId);
        repository.Delete(handler.State);
      }
      else
      {
        invocation.HandlerLogger.Info("Saving: " + handler.State.SagaId);
        repository.Save(handler.State);
      }
    }

    private ISagaStateRepository<ISagaState> GetRepositoryFor(Type sagaStateType)
    {
      object repository = _container.Resolve.All(type => 
        type.IsGenericlyCompatible(typeof(ISagaStateRepository<>).MakeGenericType(sagaStateType))).FirstOrDefault();
      if (repository == null)
      {
        throw new SagaStateRepositoryNotFoundException(sagaStateType);
      }
      return Invokers.CreateForRepository(sagaStateType, repository);
    }
  }

  public static class SagaHelpers
  {
    public static bool IsStartedBy(this HandlerInvocation invocation)
    {
      Type startedBy = typeof(ISagaStartedBy<>).MakeGenericType(invocation.MessageType);
      return startedBy.IsInstanceOfType(invocation.Handler);
    }

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
        TransportMessage transportMessage = CurrentMessageContext.Current;
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
