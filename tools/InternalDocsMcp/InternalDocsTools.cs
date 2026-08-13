using System.ComponentModel;
using ModelContextProtocol.Server;

namespace InternalDocsMcp;

[McpServerToolType]
public sealed class InternalDocsTools
{
    private readonly InternalDocsCatalog _catalog;

    public InternalDocsTools(InternalDocsCatalog catalog)
    {
        _catalog = catalog;
    }

    [McpServerTool, Description("List the sorted Markdown documents available in the allow-listed docs root.")]
    public string list_documents()
    {
        return string.Join(Environment.NewLine, _catalog.ListDocuments());
    }

    [McpServerTool, Description("Search allow-listed Markdown documents case-insensitively and return at most 20 document/line matches.")]
    public string search_docs([Description("A non-empty search phrase.")] string query)
    {
        return string.Join(
            Environment.NewLine,
            _catalog.SearchDocuments(query).Select(match => $"{match.Document}:{match.Line}: {match.Text}"));
    }

    [McpServerTool, Description("Read one allow-listed Markdown document by its file name.")]
    public string get_document([Description("A Markdown file name returned by list_documents.")] string name)
    {
        return _catalog.GetDocument(name) ?? "Document not found.";
    }
}

