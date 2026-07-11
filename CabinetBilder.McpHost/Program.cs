using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using CabinetBilder.SpaceOsBridge;
using CabinetBilder.SpaceOsBridge.Outbox;
using CabinetBilder.McpHost.Skeletons;

namespace CabinetBilder.McpHost;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Configure console logging to output to standard error (stderr)
        // because standard output (stdout) is used for the MCP stdio protocol communication.
        builder.Logging.AddConsole(consoleLogOptions =>
        {
            consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        // Setup SpaceOS bridge infrastructure
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CabinetBilder");
        builder.Services.AddSpaceOsBridge(options =>
        {
            options.BaseDirectory = appData;
            options.Authority = "https://identity.spaceos.io";
            options.ClientId = "cabinet-bilder-mcphost";
            options.Scope = "openid profile spaceos.api offline_access";
        });

        // RISK-002: az AddSpaceOsBridge beregisztrálja az OutboxWorker háttérszolgáltatást,
        // ami a VPS API-t hívná. A PoC-ban NEM akarunk kimenő hálózati forgalmat, ezért
        // eltávolítjuk a hosted service regisztrációját (az ILocalStore/IConnectionState
        // szolgáltatások megmaradnak a diagnosztikai toolokhoz).
        var outbox = builder.Services.FirstOrDefault(d => d.ImplementationType == typeof(OutboxWorker));
        if (outbox != null) builder.Services.Remove(outbox);

        // A skeleton-példányok in-memory nyilvántartása (TASK-002).
        builder.Services.AddSingleton<SkeletonRegistry>();

        // Add MCP Server and configure it with stdio transport and tools from the current assembly
        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();

        var host = builder.Build();
        await host.RunAsync();
    }
}
