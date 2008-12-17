using System;
using System.Collections.Generic;
using System.IO;

namespace Machine.Mta.DotNetBinaryStorage
{
  public interface IFlatFileSystem
  {
    bool IsFile(string path);
    Stream Open(string path);
    Stream Create(string path);
    void Delete(string path);
  }
  public class PhysicalFileSystem : IFlatFileSystem
  {
    public bool IsFile(string path)
    {
      return File.Exists(path);
    }

    public Stream Open(string path)
    {
      return File.OpenRead(path);
    }

    public Stream Create(string path)
    {
      return File.Create(path);
    }

    public void Delete(string path)
    {
      File.Delete(path);
    }
  }
  public class InMemoryFileSystem : IFlatFileSystem
  {
    readonly Dictionary<string, MemoryStream> _files = new Dictionary<string, MemoryStream>();

    public bool IsFile(string path)
    {
      lock (_files)
      {
        return _files.ContainsKey(path);
      }
    }

    public Stream Open(string path)
    {
      lock (_files)
      {
        return new MemoryStream(_files[path].ToArray());
      }
    }

    public Stream Create(string path)
    {
      lock (_files)
      {
        return _files[path] = new MemoryStream();
      }
    }

    public void Delete(string path)
    {
      if (_files.ContainsKey(path))
      {
        _files.Remove(path);
      }
    }
  }
}
