using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
      Guid[] sagaIds = invocation.RetrieveSagaIds();
      if (sagaIds.Length == 0)
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
        invocation.HandlerLogger.Info("Retrieving: " + sagaIds.JoinedStrings());
        ISagaState state = null;
        foreach (Guid sagaId in sagaIds)
        {
          state = repository.FindSagaState(sagaId);
          if (state != null)
          {
            break;
          }
        }
        if (state == null)
        {
          if (!invocation.IsStartedBy())
          {
            invocation.HandlerLogger.Warn("No state:: " + sagaIds.JoinedStrings());
            return;
          }
          invocation.HandlerLogger.Info("Starting (No State): " + sagaIds.JoinedStrings());
        }
        handler.State = state;
      }
      using (CurrentSagaContext.Open(sagaIds))
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
    
    public static Guid[] RetrieveSagaIds(this HandlerInvocation invocation)
    {
      if (invocation.IsForSagaMessage())
      {
        return new[] { invocation.SagaMessage().SagaId };
      }
      if (CurrentMessageContext.Current != null)
      {
        TransportMessage transportMessage = CurrentMessageContext.Current;
        if (transportMessage != null)
        {
          return transportMessage.SagaIds;
        }
      }
      return new Guid[0];
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

    public static string JoinedStrings(this Guid[] guids)
    {
      return guids.Select(x => x.ToString()).Join(", ");
    }

    public static string Join(this IEnumerable<string> values, string separator)
    {
      StringBuilder sb = new StringBuilder();
      foreach (string value in values)
      {
        if (sb.Length != 0)
        {
          sb.Append(separator);
        }
        sb.Append(value);
      }
      return sb.ToString();
    }
  }
}
