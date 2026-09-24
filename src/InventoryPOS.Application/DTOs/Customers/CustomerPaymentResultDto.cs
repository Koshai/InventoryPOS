namespace InventoryPOS.Application.DTOs.Customers;

public sealed class CustomerPaymentResultDto
{
    public decimal AmountReceived { get; set; }

    public decimal AmountApplied { get; set; }

    public decimal RemainingOutstanding { get; set; }

    public List<CustomerPaymentAllocationDto> Allocations
    { get; set; } = new();
}