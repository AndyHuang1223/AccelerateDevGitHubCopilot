using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using InternalDocsMcp;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: InternalDocsMcp <markdown-docs-root>");
    return 2;
}

var docsRoot = Path.GetFullPath(args[0]);
if (!Directory.Exists(docsRoot))
{
    Console.Error.WriteLine("The markdown-docs-root does not exist.");
    return 2;
}

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});
builder.Services.AddSingleton(new InternalDocsCatalog(docsRoot));
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<InternalDocsTools>();

await builder.Build().RunAsync();
return 0;
