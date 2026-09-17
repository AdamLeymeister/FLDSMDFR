using System.Diagnostics;
using System.Text;

namespace FLDSMDFR.Core.Services;

public static class VsCodeLauncher
{
    public static int Open(string path, string? searchText = null, int lineNumber = 0)
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
        int selectLength = 0;
        if (Directory.Exists(fullPath))
        {
            arguments = $"--reuse-window {Quote(fullPath)}";
        }
        else if (File.Exists(fullPath))
        {
            (int line, int column, int length) = FindHit(fullPath, searchText, lineNumber);
            arguments = $"--reuse-window -g {Quote($"{fullPath}:{line}:{column}")}";
            selectLength = length;
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

        return selectLength;
    }

    private static (int Line, int Column, int Length) FindHit(
        string filePath,
        string? searchText,
        int preferredLine)
    {
        if (preferredLine > 0 && string.IsNullOrWhiteSpace(searchText))
        {
            return (preferredLine, 1, 0);
        }

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return (1, 1, 0);
        }

        int lineNumber = 0;
        foreach (string line in File.ReadLines(filePath))
        {
            lineNumber++;
            if (preferredLine > 0 && lineNumber != preferredLine)
            {
                continue;
            }

            int index = line.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                return (lineNumber, index + 1, searchText.Length);
            }

            if (preferredLine > 0)
            {
                return (preferredLine, 1, searchText.Length);
            }
        }

        return preferredLine > 0 ? (preferredLine, 1, searchText.Length) : (1, 1, 0);
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

        return FindOnPath("code.cmd") ?? FindOnPath("code.exe") ?? FindOnPath("code");
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
