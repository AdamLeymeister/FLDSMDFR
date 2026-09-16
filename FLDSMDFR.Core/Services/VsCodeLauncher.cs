using System.Diagnostics;
using System.Text;

namespace FLDSMDFR.Core.Services;

public static class VsCodeLauncher
{
    public static void Open(string path, string? searchText = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("This row has no file path.");
        }

        string fullPath = Path.GetFullPath(path.Replace('/', Path.DirectorySeparatorChar));
        string? code = FindCodeExecutable();
        if (code == null)
        {
            throw new InvalidOperationException("VS Code was not found. Install it or add `code` to PATH.");
        }

        string arguments;
        if (Directory.Exists(fullPath))
        {
            arguments = $"--reuse-window {Quote(fullPath)}";
        }
        else if (File.Exists(fullPath))
        {
            (int line, int column) = FindHit(fullPath, searchText);
            arguments = $"--reuse-window -g {Quote($"{fullPath}:{line}:{column}")}";
        }
        else
        {
            throw new FileNotFoundException($"File not found:\n{fullPath}", fullPath);
        }

        bool shell = code.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) ||
                     code.EndsWith(".bat", StringComparison.OrdinalIgnoreCase);

        Process.Start(new ProcessStartInfo
        {
            FileName = code,
            Arguments = arguments,
            UseShellExecute = shell,
            CreateNoWindow = !shell
        });
    }

    private static (int Line, int Column) FindHit(string filePath, string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return (1, 1);
        }

        int lineNumber = 0;
        foreach (string line in File.ReadLines(filePath))
        {
            lineNumber++;
            int index = line.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                return (lineNumber, index + 1);
            }
        }

        return (1, 1);
    }

    private static string? FindCodeExecutable()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string[] candidates =
        {
            Path.Combine(localAppData, @"Programs\Microsoft VS Code\Code.exe"),
            Path.Combine(programFiles, @"Microsoft VS Code\Code.exe"),
            Path.Combine(localAppData, @"Programs\Microsoft VS Code Insiders\Code - Insiders.exe")
        };

        foreach (string candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        string? fromPath = FindOnPath("code.cmd") ?? FindOnPath("code.exe") ?? FindOnPath("code");
        return fromPath;
    }

    private static string? FindOnPath(string fileName)
    {
        string? path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        foreach (string directory in path.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }

            string candidate = Path.Combine(directory.Trim(), fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string Quote(string value)
    {
        var builder = new StringBuilder(value.Length + 2);
        builder.Append('"');
        builder.Append(value.Replace("\"", "\\\""));
        builder.Append('"');
        return builder.ToString();
    }
}
