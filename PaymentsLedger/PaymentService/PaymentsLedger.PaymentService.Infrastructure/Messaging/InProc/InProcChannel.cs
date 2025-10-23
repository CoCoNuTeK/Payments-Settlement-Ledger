using System.Threading.Channels;
using PaymentsLedger.PaymentService.Application.MessagingDefinition;

namespace PaymentsLedger.PaymentService.Infrastructure.Messaging.InProc;

internal sealed class InProcChannel : IInternalEventBus
{
    private readonly Channel<InternalMessageEnvelope> _channel;

    public InProcChannel(Channel<InternalMessageEnvelope> channel)
    {
        _channel = channel;
    }

    public async Task PublishAsync(InternalMessageEnvelope message, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(message, cancellationToken);
    }
}
