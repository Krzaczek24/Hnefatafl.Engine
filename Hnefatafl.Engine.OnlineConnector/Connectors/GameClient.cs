using Hnefatafl.Engine.OnlineConnector.Connectors.Base;
using System.Net.WebSockets;

namespace Hnefatafl.Engine.OnlineConnector.Connectors
{
    public sealed class GameClient : GameConnectorBase<ClientWebSocket>
    {
        private Uri ServerUri { get; }

        public GameClient(Uri serverUri)
        {
            ServerUri = serverUri ?? throw new ArgumentNullException(nameof(serverUri));
            Socket = new ClientWebSocket();
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (Socket?.State == WebSocketState.Open)
                return;

            await Socket!.ConnectAsync(ServerUri, cancellationToken).ConfigureAwait(false);
            ReceiveCts = new CancellationTokenSource();
            _ = Task.Run(() => ReceiveLoopAsync(ReceiveCts.Token), CancellationToken.None);
        }
    }
}
