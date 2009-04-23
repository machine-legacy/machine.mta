using System;
using System.Collections.Generic;
using Machine.Specifications;

namespace Machine.Mta.Specs
{
  [Subject("Sending and Receiving")]
  public class when_sending_message : SendingAndReceivingSpecs
  {
    Because of = () =>
    {
      for (var i = 0; i < 100; ++i)
      {
        bus.Send(message1);
      }
      System.Threading.Thread.Sleep(TimeSpan.FromSeconds(5));
    };

    It should_have_called_handler = () =>
      MessageHandler.Handled.ShouldNotBeEmpty();
  }

  public class SendingAndReceivingSpecs : DispatchSpecs
  {
    Establish context = () =>
    {
      container.Register.Type<MessageHandler>();
      messageDestinations.SendAllTo(EndpointAddress.ForLocalQueue("test"));
      bus.Start();
    };

    Cleanup after = () =>
    {
      bus.Dispose();
    };
  }

  public class MessageHandler : IConsume<IMessage>
  {
    private static readonly List<IMessage> _handled = new List<IMessage>();

    public static List<IMessage> Handled
    {
      get { return _handled; }
    }

    public void Consume(IMessage message)
    {
      _handled.Add(message);
    }
  }
}
