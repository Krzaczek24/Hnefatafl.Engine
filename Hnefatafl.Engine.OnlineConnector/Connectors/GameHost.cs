using Hnefatafl.Engine.OnlineConnector.Connectors.Base;
using System.Net;
using System.Net.WebSockets;

namespace Hnefatafl.Engine.OnlineConnector.Connectors
{
    public sealed class GameHost : GameConnectorBase<WebSocket>
    {
        private HttpListener Listener { get; }
        private CancellationTokenSource? AcceptCts { get; set; }

        public GameHost()
        {
            Listener = new HttpListener();
            Listener.Prefixes.Add("http://127.0.0.1:7777/");
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (Listener.IsListening)
                return;

            Listener.Start();
            AcceptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                var ctx = await Listener.GetContextAsync().ConfigureAwait(false);
                if (!ctx.Request.IsWebSocketRequest)
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.Close();
                    throw new InvalidOperationException("Expected WebSocket upgrade request.");
                }

                var wsContext = await ctx.AcceptWebSocketAsync(subProtocol: null).ConfigureAwait(false);
                Socket = wsContext.WebSocket;
                ReceiveCts = AcceptCts;
                _ = Task.Run(() => ReceiveLoopAsync(AcceptCts.Token), CancellationToken.None);
            }
            catch
            {
                Listener.Stop();
                throw;
            }
        }

        public override void Dispose()
        {
            if (Listener is not null)
            {
                try { Listener.Stop(); } catch { }
                try { Listener.Close(); } catch { }
            }

            base.Dispose();
            AcceptCts?.Dispose();
        }
    }
}
