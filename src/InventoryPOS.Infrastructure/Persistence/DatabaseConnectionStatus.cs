namespace InventoryPOS.Infrastructure.Persistence;

/// <summary>
/// Indicates whether a real PostgreSQL connection string was loaded
/// (installer local config or developer override).
/// </summary>
public sealed class DatabaseConnectionStatus
{
    public DatabaseConnectionStatus(bool isConfigured)
    {
        IsConfigured = isConfigured;
    }

    public bool IsConfigured { get; }
}
