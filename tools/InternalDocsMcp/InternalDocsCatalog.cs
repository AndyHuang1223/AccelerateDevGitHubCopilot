using System.Collections.ObjectModel;

namespace InternalDocsMcp;

public sealed record DocumentMatch(string Document, int Line, string Text);

public sealed class InternalDocsCatalog
{
    public const int MaxSearchResults = 20;
    private readonly string _root;

    public InternalDocsCatalog(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);

        var fullRoot = Path.GetFullPath(root);
        if (!Directory.Exists(fullRoot))
        {
            throw new DirectoryNotFoundException(fullRoot);
        }

        _root = GetFinalPath(new DirectoryInfo(fullRoot));
    }

    public IReadOnlyList<string> ListDocuments()
    {
        var documents = Directory.EnumerateFiles(_root, "*.md", SearchOption.TopDirectoryOnly)
            .Where(IsSafeFile)
            .Select(Path.GetFileName)
            .Where(name => name is not null)
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new ReadOnlyCollection<string>(documents);
    }

    public IReadOnlyList<DocumentMatch> SearchDocuments(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length > 200)
        {
            return Array.Empty<DocumentMatch>();
        }

        var matches = new List<DocumentMatch>();
        foreach (var document in ListDocuments())
        {
            var path = Path.Combine(_root, document);
            var lines = File.ReadLines(path);
            var lineNumber = 0;

            foreach (var line in lines)
            {
                lineNumber++;
                if (!line.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                matches.Add(new DocumentMatch(document, lineNumber, line.Trim()));
                if (matches.Count == MaxSearchResults)
                {
                    return matches;
                }
            }
        }

        return matches;
    }

    public string? GetDocument(string name)
    {
        var path = ResolveDocument(name);
        return path is null ? null : File.ReadAllText(path);
    }

    private string? ResolveDocument(string name)
    {
        if (string.IsNullOrWhiteSpace(name)
            || Path.IsPathRooted(name)
            || name.Contains(Path.DirectorySeparatorChar)
            || name.Contains(Path.AltDirectorySeparatorChar)
            || name.Contains("..", StringComparison.Ordinal)
            || !name.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var candidate = Path.Combine(_root, name);
        return File.Exists(candidate) && IsSafeFile(candidate) ? candidate : null;
    }

    private bool IsSafeFile(string path)
    {
        var fullPath = GetFinalPath(new FileInfo(path));
        var relative = Path.GetRelativePath(_root, fullPath);
        return relative != ".."
            && !relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            && !Path.IsPathRooted(relative);
    }

    private static string GetFinalPath(FileSystemInfo info)
    {
        var resolved = info.ResolveLinkTarget(returnFinalTarget: true);
        return Path.GetFullPath((resolved ?? info).FullName);
    }
}

