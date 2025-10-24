using System.Threading.Channels;
using PaymentsLedger.SharedKernel.Messaging;

namespace PaymentsLedger.Blazor.Infrastructure.Messaging.InProc;

internal sealed class InProcChannel(Channel<InternalMessageEnvelope> channel) : IInternalEventBus
{
    private readonly Channel<InternalMessageEnvelope> _channel = channel;

    public async Task PublishAsync(InternalMessageEnvelope message, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(message, cancellationToken);
    }
}
