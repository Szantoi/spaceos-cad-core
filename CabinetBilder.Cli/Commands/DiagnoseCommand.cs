using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using CabinetBilder.Core.Sync;
using CabinetBilder.SpaceOsBridge.TokenStorage;
using System.Reflection;

namespace CabinetBilder.Cli.Commands;

public sealed class DiagnoseCommand : Command
{
    private readonly IServiceProvider _serviceProvider;

    public DiagnoseCommand(IServiceProvider serviceProvider) : base("diagnose", "Show local system state and diagnostic information")
    {
        _serviceProvider = serviceProvider;

        var exportOption = new Option<FileInfo?>("--export", "Export diagnostic data to a JSON file");
        var outboxOption = new Option<bool>("--outbox", "Show detailed outbox information");
        var historyOption = new Option<int>("--outbox-history", () => 7, "Number of days for outbox history");

        AddOption(exportOption);
        AddOption(outboxOption);
        AddOption(historyOption);

        this.SetHandler(async (exportFile, showOutbox, days) =>
        {
            await ExecuteAsync(exportFile, showOutbox, days);
        }, exportOption, outboxOption, historyOption);
    }

    private async Task ExecuteAsync(FileInfo? exportFile, bool showOutbox, int days)
    {
        var localStore = _serviceProvider.GetRequiredService<ILocalStore>();
        var manifestManager = _serviceProvider.GetRequiredService<TenantManifestManager>();
        var connectionState = _serviceProvider.GetRequiredService<IConnectionState>();

        var statsResult = await localStore.GetStoreStatsAsync(default);
        var manifest = await manifestManager.GetManifestAsync();
        var lastSync = await localStore.GetLastSyncAtAsync(default);

        var data = new DiagnosticData
        {
            PluginVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0",
            Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            ConnectionState = connectionState.Status.ToString(),
            ActiveTenant = manifest.ActiveTenantId != null 
                ? new TenantInfo { Id = manifest.ActiveTenantId, Name = manifest.Tenants.FirstOrDefault(t => t.TenantId == manifest.ActiveTenantId)?.DisplayName ?? "Unknown" }
                : null,
            LocalStore = statsResult.IsSuccess ? statsResult.Value : null,
            LastSyncAt = lastSync.IsSuccess ? lastSync.Value : null,
            TenantsConfigured = manifest.Tenants.Count
        };

        if (showOutbox || exportFile != null)
        {
            var entriesResult = await localStore.GetOutboxEntriesAsync(days, default);
            if (entriesResult.IsSuccess)
            {
                data.OutboxEntries = entriesResult.Value.Select(e => new OutboxInfo
                {
                    Id = e.Id,
                    Operation = e.Operation.ToString(),
                    Status = e.Status.ToString(),
                    CreatedAt = e.CreatedAt,
                    RetryCount = e.RetryCount,
                    ErrorMessage = e.LastErrorMessage,
                    Payload = "[REDACTED]" // Redact sensitive payload
                }).ToList();
            }
        }

        if (exportFile != null)
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(exportFile.FullName, json);
            Console.WriteLine($"Diagnostic data exported to {exportFile.FullName}");
        }
        else
        {
            PrintToConsole(data, showOutbox);
        }
    }

    private void PrintToConsole(DiagnosticData data, bool showOutbox)
    {
        Console.WriteLine("=== CabinetBilder Diagnostics ===");
        Console.WriteLine($"Version:    {data.PluginVersion}");
        Console.WriteLine($"Runtime:    {data.Runtime}");
        Console.WriteLine($"Connection: {data.ConnectionState}");
        Console.WriteLine($"Tenant:     {data.ActiveTenant?.Name ?? "None"} ({data.ActiveTenant?.Id ?? "N/A"})");
        Console.WriteLine();
        
        if (data.LocalStore != null)
        {
            Console.WriteLine("--- Local Storage ---");
            Console.WriteLine($"Schema Ver: {data.LocalStore.SchemaVersion}");
            Console.WriteLine($"Integrity:  {data.LocalStore.IntegrityCheck}");
            Console.WriteLine($"Templates:  {data.LocalStore.TemplateCacheCount}");
            Console.WriteLine($"Materials:  {data.LocalStore.MaterialCacheCount}");
            Console.WriteLine($"Seen GUIDs: {data.LocalStore.SeenGuidsCount}");
            Console.WriteLine($"Outbox:     {data.LocalStore.OutboxPending} pending, {data.LocalStore.OutboxFailed} failed, {data.LocalStore.OutboxSucceededLast30d} succeeded (30d)");
            Console.WriteLine($"Last Sync:  {data.LastSyncAt?.ToString("g") ?? "Never"}");
        }

        if (showOutbox && data.OutboxEntries != null)
        {
            Console.WriteLine();
            Console.WriteLine("--- Recent Outbox Entries ---");
            foreach (var entry in data.OutboxEntries.Take(10))
            {
                Console.WriteLine($"{entry.CreatedAt:g} | {entry.Operation,-20} | {entry.Status,-10} | Retries: {entry.RetryCount}");
                if (!string.IsNullOrEmpty(entry.ErrorMessage))
                {
                    Console.WriteLine($"  Error: {entry.ErrorMessage}");
                }
            }
        }
    }

    private class DiagnosticData
    {
        public string PluginVersion { get; set; } = "";
        public string Runtime { get; set; } = "";
        public string ConnectionState { get; set; } = "";
        public TenantInfo? ActiveTenant { get; set; }
        public LocalStoreStats? LocalStore { get; set; }
        public DateTimeOffset? LastSyncAt { get; set; }
        public int TenantsConfigured { get; set; }
        public List<OutboxInfo>? OutboxEntries { get; set; }
    }

    private class TenantInfo
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
    }

    private class OutboxInfo
    {
        public Guid Id { get; set; }
        public string Operation { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTimeOffset CreatedAt { get; set; }
        public int RetryCount { get; set; }
        public string? ErrorMessage { get; set; }
        public string Payload { get; set; } = "";
    }
}
