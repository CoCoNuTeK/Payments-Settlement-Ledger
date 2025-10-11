namespace PaymentsLedger.PaymentService.Api.Data.Entities;

public class Payment
{
    public Guid Id { get; set; }
    public string MerchantReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

