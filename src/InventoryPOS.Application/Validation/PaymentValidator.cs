using InventoryPOS.Domain.Enums;

namespace InventoryPOS.Application.Validation;

public static class PaymentValidator
{
    public const int MaxReferenceLength = 200;
    public const int MaxNotesLength = 500;

    public static void Validate(
        PaymentMethod method,
        decimal amount,
        string? referenceNumber,
        string? notes = null)
    {
        if (!Enum.IsDefined(typeof(PaymentMethod), method))
        {
            throw new InvalidOperationException(
                "An invalid payment method was selected.");
        }

        if (amount <= 0m)
        {
            throw new InvalidOperationException(
                "Payment amount must be greater than zero.");
        }

        var reference = referenceNumber?.Trim();

        if (method != PaymentMethod.Cash &&
            string.IsNullOrWhiteSpace(reference))
        {
            throw new InvalidOperationException(
                $"A payment reference is required for {method}.");
        }

        if (reference?.Length > MaxReferenceLength)
        {
            throw new InvalidOperationException(
                $"Payment reference cannot exceed {MaxReferenceLength} characters.");
        }

        if (notes?.Trim().Length > MaxNotesLength)
        {
            throw new InvalidOperationException(
                $"Payment notes cannot exceed {MaxNotesLength} characters.");
        }
    }
}