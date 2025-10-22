namespace PaymentsLedger.PaymentService.Infrastructure.Persistence;

internal static class PaymentDbContextSeeder
{
    public static void Seed(PaymentDbContext context, bool isDesignTime)
    {
        // No-op for now. Use isDesignTime if you want to skip seeding during design-time.
    }

    public static Task SeedAsync(PaymentDbContext context, bool isDesignTime, CancellationToken cancellationToken)
    {
        // No-op for now. Use isDesignTime if you want to skip seeding during design-time.
        return Task.CompletedTask;
    }
}
