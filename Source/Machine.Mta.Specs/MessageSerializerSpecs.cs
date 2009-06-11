namespace Machine.Mta.Specs
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Text;
  using AnotherPlace;
  using Machine.Mta.InterfacesAsMessages;
  using Machine.Mta.Serializing.Xml;

  public class MessageSerializerSpecs
  {
    public void Run()
    {
      var registerer = new MessageRegisterer();
      registerer.AddMessageTypes(typeof(IPerformSingleTaskOnComputersMessage), typeof(IInstallAssemblyMessage));
      var implementations = new MessageInterfaceImplementations(new DefaultMessageInterfaceImplementationFactory(), registerer);
      var factory = new MessageFactory(implementations, new MessageDefinitionFactory());
      var assembly = factory.Create<IInstallAssemblyMessage>(m =>
      {
        m.ArtifactId = Guid.Empty;
        m.AssemblyFilename = "Jacob.exe";
      });
      var message = factory.Create<IPerformSingleTaskOnComputersMessage>(m =>
      {
        m.OperationId = Guid.NewGuid();
        m.Title = "Nothing";
        m.Message = assembly;
        m.ComputerNames = new string[0];
        m.Birthday = DateTime.UtcNow;
      });
      var formatter = new XmlTransportMessageBodyFormatter(registerer, new MtaMessageMapper(implementations, factory, registerer));
      formatter.Initialize();
      using (var stream = new MemoryStream())
      {
        formatter.Serialize(new IMessage[] { message }, stream);
        Console.WriteLine(Encoding.ASCII.GetString(stream.ToArray()));
        using (var reading = new MemoryStream(stream.ToArray()))
        {
          var read = formatter.Deserialize(reading);
        }
      }
    }
  }
  public interface IPerformSingleTaskOnComputersMessage : IMessage
  {
    Guid OperationId { get; set; }
    string[] ComputerNames { get; set; }
    string Title { get; set; }
    DateTime? Birthday { get; set; }
    IMessage Message { get; set; }
  }
}

namespace AnotherPlace
{
  using System;

  public interface IInstallAssemblyMessage : Machine.Mta.IMessage
  {
    Guid ArtifactId { get; set; }
    string AssemblyFilename { get; set; }
  }
}