using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CabinetBilder.SpaceOsBridge;
using CabinetBilder.Cli.Commands;

namespace CabinetBilder.Cli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // 1. Setup DI
        var services = new ServiceCollection();
        
        // Setup configuration (could be loaded from file, using defaults for now)
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CabinetBilder");
        
        services.AddLogging(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        // Use the same infrastructure as AutoCAD
        services.AddSpaceOsBridge(options => 
        {
            options.BaseDirectory = appData;
            options.Authority = "https://identity.spaceos.io"; // Default, can be overridden
            options.ClientId = "cabinet-bilder-cli";
            options.Scope = "openid profile spaceos.api offline_access";
        });

        var serviceProvider = services.BuildServiceProvider();

        // 2. Setup Commands
        var rootCommand = new RootCommand("CabinetBilder CLI - Advanced Cabinet Design Tools");

        rootCommand.AddCommand(new DiagnoseCommand(serviceProvider));
        rootCommand.AddCommand(new LoginCommand(serviceProvider));
        
        // Add a simple sync command
        var syncCommand = new Command("sync", "Trigger a full synchronization with SpaceOS");
        syncCommand.SetHandler(() => Console.WriteLine("Sync command not yet fully implemented in CLI."));
        rootCommand.AddCommand(syncCommand);

        // 3. Execute
        return await rootCommand.InvokeAsync(args);
    }
}
