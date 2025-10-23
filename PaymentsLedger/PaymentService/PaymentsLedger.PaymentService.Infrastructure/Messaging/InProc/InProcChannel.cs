using System.Threading.Channels;
using PaymentsLedger.PaymentService.Application.Abstractions.Messaging;

namespace PaymentsLedger.PaymentService.Infrastructure.Messaging.InProc;

internal sealed class InProcChannel : IInternalEventBus
{
    private readonly Channel<object> _channel;

    public InProcChannel(Channel<object> channel)
    {
        _channel = channel;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(@event!, cancellationToken);
    }
}
