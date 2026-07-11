using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using CabinetBilder.Core.Sync;

namespace CabinetBilder.Cli.Commands;

public sealed class LoginCommand : Command
{
    private readonly IServiceProvider _serviceProvider;

    public LoginCommand(IServiceProvider serviceProvider) : base("login", "Authenticate with SpaceOS using device code flow")
    {
        _serviceProvider = serviceProvider;

        this.SetHandler(async () =>
        {
            await ExecuteAsync();
        });
    }

    private async Task ExecuteAsync()
    {
        var auth = _serviceProvider.GetRequiredService<ISpaceOsAuthenticator>();
        
        Console.WriteLine("Initiating login...");
        var startResult = await auth.StartLoginAsync(default);

        if (!startResult.IsSuccess)
        {
            Console.WriteLine($"Error starting login: {startResult.Errors.FirstOrDefault()}");
            return;
        }

        var response = startResult.Value;
        Console.WriteLine();
        Console.WriteLine("ACTION REQUIRED");
        Console.WriteLine("---------------");
        Console.WriteLine($"1. Open your browser and go to: {response.VerificationUri}");
        Console.WriteLine($"2. Enter the following code:    {response.UserCode}");
        Console.WriteLine();
        Console.WriteLine("Waiting for authentication...");

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var completeResult = await auth.CompleteLoginAsync(response, cts.Token);

        if (completeResult.IsSuccess)
        {
            Console.WriteLine();
            Console.WriteLine("SUCCESS!");
            Console.WriteLine("Authentication complete. You are now logged in.");
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine($"Login failed: {completeResult.Errors.FirstOrDefault()}");
        }
    }
}
