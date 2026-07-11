using Microsoft.Extensions.Logging;

namespace CabinetBilder.SpaceOsBridge.Outbox;

/// <summary>
/// Implements leader election for outbox processing using an OS-level named mutex.
/// This ensures only one AutoCAD instance processes the outbox at a time.
/// Specification: Vision v2 §9.9
/// </summary>
public sealed class OutboxLeader : IDisposable
{
    private const string MutexName = @"Global\CabinetBilder.OutboxLeader";
    private readonly ILogger<OutboxLeader> _logger;
    private Mutex? _mutex;
    private bool _isLeader;

    public OutboxLeader(ILogger<OutboxLeader> logger)
    {
        _logger = logger;
    }

    public bool IsLeader => _isLeader;

    public bool TryBecomeLeader()
    {
        if (_isLeader) return true;

        try
        {
            // Note: Global\ prefix requires SeCreateGlobalPrivilege or being admin, 
            // but for per-user local session, we can use a non-global name if needed.
            // However, Vision v2 explicitly asks for Global\.
            _mutex = new Mutex(initiallyOwned: false, name: MutexName);
            
            _isLeader = _mutex.WaitOne(TimeSpan.Zero);
            
            if (_isLeader)
            {
                _logger.LogInformation("Successfully acquired OutboxLeader mutex. This instance is now the leader.");
            }
            else
            {
                _logger.LogDebug("Could not acquire OutboxLeader mutex. Another instance is already leading.");
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to OutboxLeader mutex. Falling back to non-leader mode.");
            _isLeader = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while trying to acquire OutboxLeader mutex.");
            _isLeader = false;
        }

        return _isLeader;
    }

    public void ReleaseLeader()
    {
        if (_isLeader && _mutex != null)
        {
            try
            {
                _mutex.ReleaseMutex();
                _logger.LogInformation("Released OutboxLeader mutex.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while releasing OutboxLeader mutex.");
            }
            finally
            {
                _isLeader = false;
            }
        }
    }

    public void Dispose()
    {
        ReleaseLeader();
        _mutex?.Dispose();
    }
}
