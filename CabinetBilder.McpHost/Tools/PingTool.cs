using System.ComponentModel;
using ModelContextProtocol.Server;

namespace CabinetBilder.McpHost.Tools;

[McpServerToolType]
public static class PingTool
{
    [McpServerTool(Name = "ping"), Description("Simple ping tool to check connectivity.")]
    public static object Ping()
    {
        return new { pong = true };
    }
}
