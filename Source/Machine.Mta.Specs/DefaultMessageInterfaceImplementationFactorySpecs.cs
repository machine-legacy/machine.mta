using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Machine.Mta.MessageInterfaces;
using Machine.Specifications;
using NServiceBus;

namespace Machine.Mta.Specs
{
  [Subject("Message Interfaces")]
  public class when_implementing_interfaces_with_initializer : DefaultMessageInterfaceImplementationFactorySpecs
  {
    static IAmAMessage message;

    Because of = () =>
      message = messageFactory.Create<IAmAMessage>(new { ObjectId = objectId, Name = "Jacob" });

    It should_have_message_with_properties_filled_in = () =>
      message.ObjectId.ShouldEqual(objectId);

    It should_equal_other_messages_with_the_same_data = () =>
      message.ShouldEqual(messageFactory.Create<IAmAMessage>(new { ObjectId = objectId, Name = "Jacob" }));

    It should_have_same_hash_code_as_message_with_same_data = () =>
      message.GetHashCode().ShouldEqual(messageFactory.Create<IAmAMessage>(new { ObjectId = objectId, Name = "Jacob" }).GetHashCode());
  }

  [Subject("Message Interfaces")]
  public class when_implementing_interfaces : DefaultMessageInterfaceImplementationFactorySpecs
  {
    static IAmAMessage message;

    Because of = () =>
    {
      message = messageFactory.Create<IAmAMessage>();
      message.ObjectId = objectId;
      message.Name = "Jacob";
    };

    It should_have_message_with_properties_filled_in = () =>
      message.ObjectId.ShouldEqual(objectId);

    It should_equal_other_messages_with_the_same_data = () =>
      message.ShouldEqual(messageFactory.Create<IAmAMessage>(new { ObjectId = objectId, Name = "Jacob" }));

    It should_have_same_hash_code_as_message_with_same_data = () =>
      message.GetHashCode().ShouldEqual(messageFactory.Create<IAmAMessage>(new { ObjectId = objectId, Name = "Jacob" }).GetHashCode());
  }

  [Subject("Message Interfaces")]
  public class when_implementing_interfaces_and_missing_a_property : DefaultMessageInterfaceImplementationFactorySpecs
  {
    static IAmAMessage message;

    Because of = () =>
    {
      error = Catch.Exception(() => message = messageFactory.Create<IAmAMessage>(new { Name = "Jacob" }));
    };

    It should_throw = () =>
      error.ShouldNotBeNull();
  }

  [Subject("Message Interfaces")]
  public class when_implementing_interfaces_and_has_extra_properties : DefaultMessageInterfaceImplementationFactorySpecs
  {
    static IAmAMessage message;

    Because of = () =>
    {
      error = Catch.Exception(() => message = messageFactory.Create<IAmAMessage>(new { ObjectId = objectId, Name = "Jacob", FirstName = "Jacob" }));
    };

    It should_throw = () =>
      error.ShouldNotBeNull();
  }

  [Subject("Message Interfaces")]
  public class with_two_equal_messages : DefaultMessageInterfaceImplementationFactorySpecs
  {
    static IAmAMessage message1;
    static IAmAMessage message2;

    Because of = () =>
    {
      message1 = messageFactory.Create<IAmAMessage>(new { ObjectId = objectId, Name = "Jacob" });
      message2 = messageFactory.Create<IAmAMessage>(new { ObjectId = objectId, Name = "Jacob" });
    };

    It should_be_equal = () =>
      message1.ShouldEqual(message2);

    It should_have_same_hash_code = () =>
      message1.GetHashCode().ShouldEqual(message2.GetHashCode());

    It should_generate_expected_string = () =>
      message1.ToString().ShouldEqual("IAmAMessage<ObjectId=" + objectId + ", Name=Jacob>");
  }

  [Subject("Message Interfaces")]
  public class with_two_equal_messages_with_children : DefaultMessageInterfaceImplementationFactorySpecs
  {
    static IHaveAChildMessage message1;
    static IHaveAChildMessage message2;

    Because of = () =>
    {
      var child1 = messageFactory.Create<IAmAMessage>(new { ObjectId = objectId, Name = "Jacob" });
      var child2 = messageFactory.Create<IAmAMessage>(new { ObjectId = objectId, Name = "Jacob" });
      message1 = messageFactory.Create<IHaveAChildMessage>(new { Child = child1 });
      message2 = messageFactory.Create<IHaveAChildMessage>(new { Child = child2 });
    };

    It should_be_equal = () =>
      message1.ShouldEqual(message2);

    It should_have_same_hash_code = () =>
      message1.GetHashCode().ShouldEqual(message2.GetHashCode());
  }

  [Subject("Message Interfaces")]
  public class with_two_inequal_messages_with_children : DefaultMessageInterfaceImplementationFactorySpecs
  {
    static IHaveAChildMessage message1;
    static IHaveAChildMessage message2;

    Because of = () =>
    {
      var child1 = messageFactory.Create<IAmAMessage>(new { ObjectId = objectId, Name = "Jacob" });
      var child2 = messageFactory.Create<IAmAMessage>(new { ObjectId = objectId, Name = "Andy" });
      message1 = messageFactory.Create<IHaveAChildMessage>(new { Child = child1 });
      message2 = messageFactory.Create<IHaveAChildMessage>(new { Child = child2 });
    };

    It should_not_be_equal = () =>
      message1.ShouldNotEqual(message2);

    It should_not_have_same_hash_code = () =>
      message1.GetHashCode().ShouldNotEqual(message2.GetHashCode());
  }

  [Subject("Message Interfaces")]
  public class with_two_inequal_messages : DefaultMessageInterfaceImplementationFactorySpecs
  {
    static IAmAMessage message1;
    static IAmAMessage message2;

    Because of = () =>
    {
      message1 = messageFactory.Create<IAmAMessage>(new { ObjectId = objectId, Name = "Jacob" });
      message2 = messageFactory.Create<IAmAMessage>(new { ObjectId = objectId, Name = "Andy" });
    };

    It should_not_be_equal = () =>
      message1.ShouldNotEqual(message2);

    It should_not_have_same_hash_code = () =>
      message1.GetHashCode().ShouldNotEqual(message2.GetHashCode());
  }

  [Subject("Message Interfaces")]
  public class with_two_inequal_messages_one_with_a_null_attribute : DefaultMessageInterfaceImplementationFactorySpecs
  {
    static IAmAMessage message1;
    static IAmAMessage message2;

    Because of = () =>
    {
      message1 = messageFactory.Create<IAmAMessage>(new { ObjectId = objectId, Name = "Jacob" });
      message2 = messageFactory.Create<IAmAMessage>();
    };

    It should_not_be_equal = () =>
      message1.ShouldNotEqual(message2);

    It should_not_have_same_hash_code = () =>
      message1.GetHashCode().ShouldNotEqual(message2.GetHashCode());
  }

  [Subject("Message Interfaces")]
  public class with_a_message_and_a_random_object : DefaultMessageInterfaceImplementationFactorySpecs
  {
    static IAmAMessage message1;
    static object randomObject;

    Because of = () =>
    {
      message1 = messageFactory.Create<IAmAMessage>(new { ObjectId = objectId, Name = "Jacob" });
      randomObject = new object();
    };

    It should_not_be_equal = () =>
      message1.ShouldNotEqual(randomObject);

    It should_not_have_same_hash_code = () =>
      message1.GetHashCode().ShouldNotEqual(randomObject.GetHashCode());
  }

  [Subject("Message Interfaces")]
  public class with_a_message_and_a_null_value : DefaultMessageInterfaceImplementationFactorySpecs
  {
    static IAmAMessage message1;
    static object randomObject;

    Because of = () =>
    {
      message1 = messageFactory.Create<IAmAMessage>(new { ObjectId = objectId, Name = "Jacob" });
      randomObject = null;
    };

    It should_not_be_equal = () =>
      message1.ShouldNotEqual(randomObject);
  }

  [Subject("Message Interfaces")]
  public class with_a_message_with_nullable_types : DefaultMessageInterfaceImplementationFactorySpecs
  {
    static IComplexMessage message1;
    static IComplexMessage message2;
    static DateTime now = DateTime.UtcNow;

    Because of = () =>
    {
      message1 = messageFactory.Create<IComplexMessage>(new {
        Id = objectId,
        Integer = 1,
        DateTime = now,
        NullableDateTime = now,
        ArrayOfEnums = new [] { DayOfWeek.Tuesday }
      });
      message2 = messageFactory.Create<IComplexMessage>(new {
        Id = objectId,
        Integer = 1,
        DateTime = now,
        NullableDateTime = now,
        ArrayOfEnums = new [] { DayOfWeek.Tuesday }
      });
    };

    It should_be_equal = () =>
      message1.ShouldEqual(message2);

    It should_have_the_same_hash_code = () =>
      message1.GetHashCode().ShouldEqual(message2.GetHashCode());

    It should_generate_an_awesome_string = () =>
      message1.ToString().ShouldEqual("IComplexMessage<Id=" + objectId + ", Integer=1, DateTime=" + now + ", NullableDateTime=" + now + ", ArrayOfEnums=[Tuesday]>");
  }

  [Subject("Message Interfaces")]
  public class with_a_message_with_nullable_types_that_are_null : DefaultMessageInterfaceImplementationFactorySpecs
  {
    static IComplexMessage message1;
    static IComplexMessage message2;
    static DateTime now = DateTime.UtcNow;

    Because of = () =>
    {
      message1 = messageFactory.Create<IComplexMessage>(m => {
        m.Id = objectId;
        m.Integer = 1;
        m.DateTime = now;
        m.NullableDateTime = null;
        m.ArrayOfEnums = new [] { DayOfWeek.Tuesday };
      });
      message2 = messageFactory.Create<IComplexMessage>(m => {
        m.Id = objectId;
        m.Integer = 1;
        m.DateTime = now;
        m.NullableDateTime = null;
        m.ArrayOfEnums = new [] { DayOfWeek.Tuesday };
      });
    };

    It should_be_equal = () =>
      message1.ShouldEqual(message2);

    It should_have_the_same_hash_code = () =>
      message1.GetHashCode().ShouldEqual(message2.GetHashCode());

    It should_generate_an_awesome_string = () =>
      message1.ToString().ShouldEqual("IComplexMessage<Id=" + objectId + ", Integer=1, DateTime=" + now + ", NullableDateTime=(null), ArrayOfEnums=[Tuesday]>");
  }

  [Subject("Message Interfaces")]
  public class with_a_message_with_no_setters : DefaultMessageInterfaceImplementationFactorySpecs
  {
    static IAnotherMessage message1;
    static IAnotherMessage message2;

    Because of = () =>
    {
      message1 = messageFactory.Create<IAnotherMessage>(new {
        ObjectId = objectId,
        Integer = 1,
        DayOfWeek = DayOfWeek.Tuesday
      });
      message2 = messageFactory.Create<IAnotherMessage>(new {
        ObjectId = objectId,
        Integer = 1,
        DayOfWeek = DayOfWeek.Tuesday
      });
    };

    It should_be_equal = () =>
      message1.ShouldEqual(message2);
  }

  public class DefaultMessageInterfaceImplementationFactorySpecs
  {
    protected static DefaultMessageInterfaceImplementationFactory factory;
    protected static MessageFactory messageFactory;
    protected static Guid objectId = Guid.NewGuid();
    protected static Exception error;

    Establish context = () =>
    {
      messageFactory = new MessageFactory();
      messageFactory.Initialize(new[] { typeof(IAmAMessage), typeof(IHaveAChildMessage), typeof(ISampleMessage), typeof(IComplexMessage), typeof(IAnotherMessage) });
    };
  }

  public interface IAmAMessage : IMessage
  {
    Guid ObjectId { get; set; }
    string Name { get; set; }
  }

  public interface IHaveAChildMessage : IMessage
  {
    IAmAMessage Child { get; set; }
  }

  public interface IComplexMessage : IMessage
  {
    Guid Id { get; set; }
    Int32 Integer { get; set; }
    DateTime DateTime { get; set; }
    DateTime? NullableDateTime { get; set; }
    DayOfWeek[] ArrayOfEnums { get; set; }
  }

  public interface IAnotherMessage : IMessage
  {
    Guid ObjectId { get; }
    Int32 Integer { get; }
    DayOfWeek DayOfWeek { get; }
  }

  public interface ISampleMessage : IMessage
  {
  }
}
