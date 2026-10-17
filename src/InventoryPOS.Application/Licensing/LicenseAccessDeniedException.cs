namespace InventoryPOS.Application.Licensing;

/// <summary>
/// Thrown when a write is rejected because the license is not usable.
/// Callers may treat this as a definite non-commit: no durable server
/// mutation occurred for the attempted operation.
/// </summary>
public sealed class LicenseAccessDeniedException : InvalidOperationException
{
    public LicenseAccessDeniedException(string message)
        : base(message)
    {
    }
}
