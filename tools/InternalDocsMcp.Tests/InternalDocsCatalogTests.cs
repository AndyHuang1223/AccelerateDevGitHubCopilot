using InternalDocsMcp;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Xunit;

namespace InternalDocsMcp.Tests;

public sealed class InternalDocsCatalogTests
{
    [Fact]
    public void ListDocuments_ReturnsSortedMarkdownFilesOnly()
    {
        using var fixture = new DocsFixture();
        fixture.Write("z-last.md", "z");
        fixture.Write("a-first.md", "a");
        fixture.Write("ignored.txt", "ignore");
        var catalog = new InternalDocsCatalog(fixture.Root);

        Assert.Equal(["a-first.md", "z-last.md"], catalog.ListDocuments());
    }

    [Fact]
    public void SearchDocuments_IsCaseInsensitiveAndBounded()
    {
        using var fixture = new DocsFixture();
        for (var index = 0; index < 25; index++)
        {
            fixture.Write($"doc-{index:00}.md", "Architecture rule");
        }

        var matches = new InternalDocsCatalog(fixture.Root).SearchDocuments("ARCHITECTURE");

        Assert.Equal(InternalDocsCatalog.MaxSearchResults, matches.Count);
        Assert.All(matches, match => Assert.Equal("Architecture rule", match.Text));
    }

    [Fact]
    public void SearchDocuments_EmptyOrTooLongQueryReturnsNoResults()
    {
        using var fixture = new DocsFixture();
        fixture.Write("doc.md", "Architecture rule");
        var catalog = new InternalDocsCatalog(fixture.Root);

        Assert.Empty(catalog.SearchDocuments(" "));
        Assert.Empty(catalog.SearchDocuments(new string('x', 201)));
    }

    [Fact]
    public void GetDocument_RejectsUnknownNonMarkdownAndTraversalNames()
    {
        using var fixture = new DocsFixture();
        fixture.Write("doc.md", "safe");
        fixture.Write("secret.txt", "secret");
        var catalog = new InternalDocsCatalog(fixture.Root);

        Assert.Equal("safe", catalog.GetDocument("doc.md"));
        Assert.Null(catalog.GetDocument("secret.txt"));
        Assert.Null(catalog.GetDocument("../doc.md"));
        Assert.Null(catalog.GetDocument(Path.Combine(fixture.Root, "doc.md")));
    }

    [Fact]
    public void ListAndGet_RejectSymlinkOutsideRoot()
    {
        using var fixture = new DocsFixture();
        fixture.Write("safe.md", "safe");
        var outside = Path.Combine(Path.GetTempPath(), $"outside-{Guid.NewGuid():N}.md");
        File.WriteAllText(outside, "outside");

        try
        {
            Directory.CreateDirectory(fixture.Root);
            File.CreateSymbolicLink(Path.Combine(fixture.Root, "outside.md"), outside);
        }
        catch (UnauthorizedAccessException)
        {
            File.Delete(outside);
            return;
        }
        catch (IOException ex) when (OperatingSystem.IsWindows() && ex.HResult == unchecked((int)0x80070522))
        {
            File.Delete(outside);
            return;
        }
        catch (PlatformNotSupportedException)
        {
            File.Delete(outside);
            return;
        }

        try
        {
            var catalog = new InternalDocsCatalog(fixture.Root);
            Assert.Equal(["safe.md"], catalog.ListDocuments());
            Assert.Null(catalog.GetDocument("outside.md"));
        }
        finally
        {
            File.Delete(outside);
        }
    }

    [Fact]
    public async Task StdioServer_ExposesTheThreeReadOnlyTools()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        var serverPath = Path.Combine(repoRoot, "tools", "InternalDocsMcp", "bin", "Debug", "net8.0", "InternalDocsMcp.dll");
        var docsRoot = Path.Combine(repoRoot, "training", "internal-docs");
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "Internal Docs Test Server",
            Command = "dotnet",
            Arguments = [serverPath, docsRoot]
        });

        await using var client = await McpClient.CreateAsync(transport);
        var tools = await client.ListToolsAsync();

        Assert.Equal(
            ["get_document", "list_documents", "search_docs"],
            tools.Select(tool => tool.Name).OrderBy(name => name, StringComparer.Ordinal));

        var result = await client.CallToolAsync(
            "search_docs",
            new Dictionary<string, object?> { ["query"] = "ProblemDetails" },
            cancellationToken: CancellationToken.None);

        Assert.Contains(result.Content.OfType<TextContentBlock>(), block => block.Text.Contains("api-guidelines.md", StringComparison.Ordinal));
    }

    private sealed class DocsFixture : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "internal-docs-mcp-tests", Guid.NewGuid().ToString("N"));

        public void Write(string name, string content)
        {
            Directory.CreateDirectory(Root);
            File.WriteAllText(Path.Combine(Root, name), content);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
