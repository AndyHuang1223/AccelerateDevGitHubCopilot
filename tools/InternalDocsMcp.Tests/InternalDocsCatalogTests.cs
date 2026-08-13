using InternalDocsMcp;
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
