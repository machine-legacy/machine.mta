using System;
using System.Collections.Generic;
using System.Linq;

using Machine.Mta.Sagas;
using Machine.Specifications;

namespace Machine.Mta.Specs.Generics
{
  class ReadsMessage : IConsume<IMessage> { public void Consume(IMessage message) { } }
  class ReadsSmallerMessage : IConsume<IAmSmallerMessage> { public void Consume(IAmSmallerMessage message) { } }
  class ReadsEvenSmallerMessage : IConsume<IAmEvenSmallerMessage> { public void Consume(IAmEvenSmallerMessage message) { } }
  class ReadsTwoMessages : IConsume<IMessage>, IConsume<IAmSmallerMessage>, IDisposable
  {
    public void Consume(IMessage message) { }
    public void Consume(IAmSmallerMessage message) { }
    public void Dispose() { }
  }

  public interface IAmSmallerMessage : IMessage
  {
  }

  public interface IAmEvenSmallerMessage : IAmSmallerMessage
  {
  }

  public interface IMap<A, B> { }

  public class Map1 : IMap<string, string> { }
  public class Map2 : IMap<string, string>, IMap<object, string> { }
  public class Map3 : Map1, IMap<short, short> { }

  [Subject("Generics")]
  public class when_enumerating_generic_variations_when_there_are_none
  {
    It should_be_empty = () =>
      typeof(String).AllGenericVariations(typeof(IConsume<>)).ToArray().ShouldBeEmpty();
  }

  [Subject("Generics")]
  public class when_enumerating_generic_variations_of_actual_generic_type
  {
    It should_contain_the_type = () =>
      typeof(IConsume<IMessage>).AllGenericVariations(typeof(IConsume<>)).ToArray().ShouldContainOnly(typeof(IConsume<IMessage>));
  }

  [Subject("Generics")]
  public class when_enumerating_generic_variations_of_implementing_type
  {
    It should_contain_the_type = () =>
      typeof(ReadsMessage).AllGenericVariations(typeof(IConsume<>)).ToArray().ShouldContainOnly(typeof(IConsume<IMessage>));
  }

  [Subject("Generics")]
  public class when_enumerating_generic_variations_of_type_implementing_twice
  {
    It should_contain_the_types = () =>
      typeof(ReadsTwoMessages).AllGenericVariations(typeof(IConsume<>)).ToArray().ShouldContainOnly(typeof(IConsume<IMessage>), typeof(IConsume<IAmSmallerMessage>));
  }

  [Subject("Generics")]
  public class when_enumerating_two_parameter_generic_variations_of_type
  {
    It should_contain_the_type = () =>
      typeof(Map1).AllGenericVariations(typeof(IMap<,>)).ToArray().ShouldContainOnly(typeof(IMap<string, string>));
  }

  [Subject("Generics")]
  public class when_enumerating_two_parameter_generic_variations_of_type_implementing_twice
  {
    It should_contain_the_types = () =>
      typeof(Map3).AllGenericVariations(typeof(IMap<,>)).Distinct().ToArray().ShouldContainOnly(
        typeof(IMap<string, string>),
        typeof(IMap<short, short>)
      );
  }

  [Subject("Generics")]
  public class when_getting_bigger_types_with_type_that_takes_bigger_type
  {
    static IEnumerable<Type> types;

    Establish context = () =>
      types = typeof(ReadsMessage).AllGenericVariations(typeof(IConsume<>));

    It should_contain_bigger_type_of_bigger_type = () =>
      types.BiggerThan(typeof(IConsume<IMessage>)).Distinct().ToArray().ShouldContainOnly(
        typeof(IConsume<IMessage>)
      );

    It should_contain_bigger_type_of_smaller_type = () =>
      types.BiggerThan(typeof(IConsume<IAmSmallerMessage>)).Distinct().ToArray().ShouldContainOnly(
        typeof(IConsume<IMessage>)
      );
  }

  [Subject("Generics")]
  public class when_getting_bigger_types_with_type_that_takes_smaller_type
  {
    static IEnumerable<Type> types;

    Establish context = () =>
      types = typeof(ReadsSmallerMessage).AllGenericVariations(typeof(IConsume<>));

    It should_contain_nothing_bigger = () =>
      types.BiggerThan(typeof(IConsume<IMessage>)).Distinct().ToArray().ShouldBeEmpty();

    It should_contain_smaller_type = () =>
      types.BiggerThan(typeof(IConsume<IAmSmallerMessage>)).Distinct().ToArray().ShouldContainOnly(
        typeof(IConsume<IAmSmallerMessage>)
      );
  }

  [Subject("Generics")]
  public class when_getting_smaller_type_of_messages
  {
    static IEnumerable<Type> types;

    Establish context = () =>
      types = typeof(ReadsTwoMessages).AllGenericVariations(typeof(IConsume<>));

    It should_contain_smaller_type = () =>
      types.SmallerType().ShouldEqual(typeof(IConsume<IAmSmallerMessage>));
  }

  [Subject("Generics")]
  public class when_getting_smaller_type
  {
    It should_choose_string_over_object = () =>
      new[] { typeof(Ignore<string>), typeof(Ignore<object>) }.SmallerType().ShouldEqual(typeof(Ignore<string>));

    It should_even_smaller_message = () =>
      new[] {
        typeof(Ignore<IMessage>),
        typeof(Ignore<IAmSmallerMessage>),
        typeof(Ignore<IAmEvenSmallerMessage>)
      }.SmallerType().ShouldEqual(typeof(Ignore<IAmEvenSmallerMessage>));

    It should_even_smaller_message_after_getting_those_that_are_bigger = () =>
      new[] {
        typeof(Ignore<IMessage>),
        typeof(Ignore<ISagaMessage>),
        typeof(Ignore<IAmSmallerMessage>),
        typeof(Ignore<IAmEvenSmallerMessage>)
      }.BiggerThan(typeof(Ignore<IAmEvenSmallerMessage>)).SmallerType().ShouldEqual(typeof(Ignore<IAmEvenSmallerMessage>));
  }

  public interface Ignore<A> { }
}
