using System;
using InventoryPOS.Application.DTOs.Customers;

namespace InventoryPOS.Desktop.Services;

public sealed class PendingCustomerPayment
{
    public bool IsInvoicePayment { get; set; }

    public Guid? TargetSaleId { get; set; }

    public ReceiveCustomerPaymentRequest Request { get; set; } = new();
}