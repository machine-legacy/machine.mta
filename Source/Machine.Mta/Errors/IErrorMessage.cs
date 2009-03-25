using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Machine.Mta.Errors
{
  public interface IErrorMessage : IMessage
  {
    DateTime OccuredAt { get; set; }
    ProcessInformation Process { get; set; }
    EnvironmentInformation[] Environments { get; set; }
    EndpointAddress Address { get; set; }
    TransportMessage TransportMessage { get; set; }
    Exception Error { get; set; }
  }

  public class EnvironmentInformation
  {
    readonly string _name;
    readonly Dictionary<string, string> _values;

    public string Name
    {
      get { return _name; }
    }

    public Dictionary<string, string> Values
    {
      get { return _values; }
    }

    public EnvironmentInformation(string name, Dictionary<string, string> values)
    {
      _name = name;
      _values = values;
    }
  }

  public class ProcessInformation
  {
    Int32 _id;
    DateTime _startTime;
    string _processName;
    string _machineName;
    string _fileName;
    string _fileVersion;
    long _pagedMemorySize64;
    long _pagedSystemMemorySize64;
    long _peakPagedMemorySize64;
    long _peakVirtualMemorySize64;
    long _peakWorkingSet64;
    long _privateMemorySize64;
    long _virtualMemorySize64;
    long _workingSet64;
    TimeSpan _totalProcessorTime;
    TimeSpan _userProcessorTime;
    TimeSpan _privilegedProcessorTime;

    public Int32 Id
    {
      get { return _id; }
      set { _id = value; }
    }

    public DateTime StartTime
    {
      get { return _startTime; }
      set { _startTime = value; }
    }

    public string ProcessName
    {
      get { return _processName; }
      set { _processName = value; }
    }

    public string MachineName
    {
      get { return _machineName; }
      set { _machineName = value; }
    }

    public string FileName
    {
      get { return _fileName; }
      set { _fileName = value; }
    }

    public string FileVersion
    {
      get { return _fileVersion; }
      set { _fileVersion = value; }
    }

    public long PagedMemorySize64
    {
      get { return _pagedMemorySize64; }
      set { _pagedMemorySize64 = value; }
    }

    public long PagedSystemMemorySize64
    {
      get { return _pagedSystemMemorySize64; }
      set { _pagedSystemMemorySize64 = value; }
    }

    public long PeakPagedMemorySize64
    {
      get { return _peakPagedMemorySize64; }
      set { _peakPagedMemorySize64 = value; }
    }

    public long PeakVirtualMemorySize64
    {
      get { return _peakVirtualMemorySize64; }
      set { _peakVirtualMemorySize64 = value; }
    }

    public long PeakWorkingSet64
    {
      get { return _peakWorkingSet64; }
      set { _peakWorkingSet64 = value; }
    }

    public long PrivateMemorySize64
    {
      get { return _privateMemorySize64; }
      set { _privateMemorySize64 = value; }
    }

    public long VirtualMemorySize64
    {
      get { return _virtualMemorySize64; }
      set { _virtualMemorySize64 = value; }
    }

    public long WorkingSet64
    {
      get { return _workingSet64; }
      set { _workingSet64 = value; }
    }

    public TimeSpan TotalProcessorTime
    {
      get { return _totalProcessorTime; }
      set { _totalProcessorTime = value; }
    }

    public TimeSpan UserProcessorTime
    {
      get { return _userProcessorTime; }
      set { _userProcessorTime = value; }
    }

    public TimeSpan PrivilegedProcessorTime
    {
      get { return _privilegedProcessorTime; }
      set { _privilegedProcessorTime = value; }
    }
  }

  public static class ErrorMessageBuilder
  {
    public static IErrorMessage ErrorMessage(this IMessageFactory factory, DateTime occuredAt, Exception error, EndpointAddress address, TransportMessage transportMessage, params EnvironmentInformation[] environments)
    {
      ProcessInformation process = Process.GetCurrentProcess().ToInformation();
      return factory.Create<IErrorMessage>(new {
        OccuredAt = occuredAt,
        Process = process,
        Environments = environments,
        Address = address,
        TransportMessage = transportMessage,
        Error = error
      });
    }

    public static IErrorMessage ErrorMessage(this IMessageFactory factory, DateTime occuredAt, Exception error, params EnvironmentInformation[] environments)
    {
      return ErrorMessage(factory, occuredAt, error, null, null, environments);
    }

    public static ProcessInformation ToInformation(this Process process)
    {
      return new ProcessInformation {
        Id = process.Id,
        StartTime = process.StartTime,
        ProcessName = process.ProcessName,
        MachineName = process.MachineName,
        FileName = process.MainModule == null ? String.Empty : process.MainModule.FileName,
        FileVersion = process.MainModule == null ? String.Empty : process.MainModule.FileVersionInfo.FileVersion,
        PagedMemorySize64 = process.PagedMemorySize64,
        PagedSystemMemorySize64 = process.PagedSystemMemorySize64,
        PeakPagedMemorySize64 = process.PeakPagedMemorySize64,
        PeakVirtualMemorySize64 = process.PeakVirtualMemorySize64,
        PeakWorkingSet64 = process.PeakWorkingSet64,
        PrivateMemorySize64 = process.PrivateMemorySize64,
        VirtualMemorySize64 = process.VirtualMemorySize64,
        WorkingSet64 = process.WorkingSet64,
        TotalProcessorTime = process.TotalProcessorTime,
        UserProcessorTime = process.UserProcessorTime,
        PrivilegedProcessorTime = process.PrivilegedProcessorTime
      };
    }
  }
}
