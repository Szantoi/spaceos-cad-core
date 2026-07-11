using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using CabinetBilder.Core.Sync;
using CabinetBilder.McpHost.Serialization;

namespace CabinetBilder.McpHost.Tools;

/// <summary>
/// Diagnosztikai MCP toolok. Az ILocalStore és IConnectionState DI-ből injektálódik
/// a tool-metódusokba (a SpaceOsBridge regisztrálja őket).
/// </summary>
[McpServerToolType]
public static class DiagnosticsTools
{
    [McpServerTool(Name = "get_store_stats"), Description("Lekérdezi a lokális SQLite tároló statisztikáit és épségét (ILocalStore.GetStoreStatsAsync).")]
    public static async Task<McpToolResponse<object>> GetStoreStats(ILocalStore store, CancellationToken ct = default)
    {
        var result = await store.GetStoreStatsAsync(ct);
        if (result.IsSuccess)
        {
            var s = result.Value;
            return new McpToolResponse<object>
            {
                IsSuccess = true,
                Status = "Ok",
                Value = new
                {
                    schemaVersion = s.SchemaVersion,
                    integrityCheck = s.IntegrityCheck,
                    templateCacheCount = s.TemplateCacheCount,
                    materialCacheCount = s.MaterialCacheCount,
                    seenGuidsCount = s.SeenGuidsCount,
                    outboxPending = s.OutboxPending,
                    outboxSucceededLast30d = s.OutboxSucceededLast30d,
                    outboxFailed = s.OutboxFailed
                }
            };
        }
        return new McpToolResponse<object>
        {
            IsSuccess = false,
            Status = "Error",
            Errors = result.Errors.ToList(),
            Value = null
        };
    }

    [McpServerTool(Name = "get_connection_status"), Description("Lekéri a SpaceOS cloud kapcsolódási státuszát és a bejelentkezett felhasználó adatait.")]
    public static McpToolResponse<object> GetConnectionStatus(IConnectionState state)
    {
        return new McpToolResponse<object>
        {
            IsSuccess = true,
            Status = "Ok",
            Value = new
            {
                status = state.Status.ToString(),
                activeTenantId = state.ActiveTenantId,
                userDisplayName = state.UserDisplayName,
                lastSyncTime = state.LastSyncTime
            }
        };
    }
}
