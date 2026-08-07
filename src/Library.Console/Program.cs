using Microsoft.Extensions.DependencyInjection;
using Library.Infrastructure.Data;
using Library.ApplicationCore;
using Microsoft.Extensions.Configuration;

var services = new ServiceCollection();

var configuration = new ConfigurationBuilder()
// Resolve configuration relative to the compiled output directory.
.SetBasePath(AppContext.BaseDirectory)
.AddJsonFile("appSettings.json")
.Build();

services.AddSingleton<IConfiguration>(configuration);

services.AddScoped<IPatronRepository, JsonPatronRepository>();
services.AddScoped<ILoanRepository, JsonLoanRepository>();
services.AddScoped<ILoanService, LoanService>();
services.AddScoped<IPatronService, PatronService>();

services.AddSingleton<JsonData>();
services.AddSingleton<ConsoleApp>();

var servicesProvider = services.BuildServiceProvider();

var resetData = args.Any(arg => string.Equals(arg, "--reset-data", StringComparison.OrdinalIgnoreCase));
var jsonData = servicesProvider.GetRequiredService<JsonData>();

try
{
    await jsonData.InitializeAsync(resetData);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Unable to initialize library data: {ex.Message}");
    Environment.ExitCode = 1;
    return;
}

Console.WriteLine($"Using library data: {jsonData.RuntimeDataRootPath}");

var consoleApp = servicesProvider.GetRequiredService<ConsoleApp>();
await consoleApp.Run();
