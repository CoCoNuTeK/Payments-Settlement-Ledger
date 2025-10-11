namespace PaymentsLedger.MerchantService.Api.Data.Entities;

public class Merchant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

