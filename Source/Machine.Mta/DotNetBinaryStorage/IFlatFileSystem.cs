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
    IEnumerable<string> ListFiles(string directory, string suffix);
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

    public IEnumerable<string> ListFiles(string directory, string suffix)
    {
      return Directory.GetFiles(directory, "*." + suffix);
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
      lock (_files)
      {
        if (_files.ContainsKey(path))
        {
          _files.Remove(path);
        }
      }
    }

    public IEnumerable<string> ListFiles(string directory, string suffix)
    {
      List<string> matched = new List<string>();
      lock (_files)
      {
        foreach (KeyValuePair<string, MemoryStream> pair in _files)
        {
          string entryDirectory = Path.GetDirectoryName(pair.Key);
          string extension = Path.GetExtension(pair.Key);
          if (!entryDirectory.Equals(Path.GetDirectoryName(directory), StringComparison.InvariantCultureIgnoreCase))
          {
            continue;
          }
          if (!extension.Equals("." + suffix, StringComparison.InvariantCultureIgnoreCase))
          {
            continue;
          }
          matched.Add(pair.Key);
        }
      }
      return matched;
    }
  }
}
