namespace FLDSMDFR.Core.Services;

public sealed class SourceTextCache
{
    private readonly Dictionary<string, string[]> _files = new(StringComparer.OrdinalIgnoreCase);

    public void Clear()
    {
        _files.Clear();
    }

    public void Load(IEnumerable<string> paths)
    {
        foreach (string path in paths)
        {
            if (string.IsNullOrWhiteSpace(path) || _files.ContainsKey(path))
            {
                continue;
            }

            _files[path] = TryRead(path);
        }
    }

    public string[] GetLines(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Array.Empty<string>();
        }

        if (_files.TryGetValue(path, out string[]? lines))
        {
            return lines;
        }

        string[] loaded = TryRead(path);
        _files[path] = loaded;
        return loaded;
    }

    public string GetLine(string path, int lineNumber)
    {
        string[] lines = GetLines(path);
        if (lineNumber < 1 || lineNumber > lines.Length)
        {
            return string.Empty;
        }

        return lines[lineNumber - 1].TrimEnd('\r', '\n');
    }

    public bool HasFile(string path)
    {
        return GetLines(path).Length > 0;
    }

    private static string[] TryRead(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                return File.ReadAllLines(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return Array.Empty<string>();
    }
}
