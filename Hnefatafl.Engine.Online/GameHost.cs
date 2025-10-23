using Hnefatafl.Engine.Online.Interfaces;
using Hnefatafl.Engine.Online.Models;
using System.Buffers;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace Hnefatafl.Engine.Online
{
    /// <summary>
    /// Accepts a single WebSocket client and exposes simple send/receive
    /// operations for typed moves (TMove). Incoming moves are queued and
    /// can be awaited using <see cref="WaitForOpponentMove"/>.
    /// </summary>
    public sealed class GameHost : IGameConnector
    {
        private HttpListener Listener { get; }
        private Channel<MoveInfo> Incoming { get; } = Channel.CreateBounded<MoveInfo>(1);
        private ArrayPool<byte> BufferPool { get; } = ArrayPool<byte>.Shared;
        private CancellationTokenSource? AcceptCts { get; set; }
        private WebSocket? Socket { get; set; }
        private bool Disposed { get; set; }

        /// <summary>
        /// Create a host
        /// </summary>
        public GameHost()
        {
            Listener = new HttpListener();
            Listener.Prefixes.Add("http://127.0.0.1:7777/");
        }

        /// <summary>
        /// Start listening and accept a single websocket client.
        /// Call before SendMyMove/WaitForOpponentMove.
        /// </summary>
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
                _ = Task.Run(() => ReceiveLoopAsync(AcceptCts.Token), CancellationToken.None);
            }
            catch
            {
                Listener.Stop();
                throw;
            }
        }

        /// <summary>
        /// Send a move to connected client. This method sends the JSON text frame
        /// and returns after the frame is queued/sent; it does NOT wait for any reply.
        /// </summary>
        public async Task SendMyMove(MoveInfo move)
        {
            ThrowIfDisposed();
            if (Socket is null || Socket.State != WebSocketState.Open)
                throw new InvalidOperationException("No connected client.");

            string text = move.Serialize();
            var bytes = Encoding.UTF8.GetBytes(text);

            try
            {
                await Socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken: CancellationToken.None).ConfigureAwait(false);
            }
            catch (WebSocketException ex)
            {
                // If send fails, rethrow a clearer exception
                throw new IOException("Failed to send move to client.", ex);
            }
        }

        /// <summary>
        /// Waits asynchronously for the next opponent move (TMove) sent by the remote peer.
        /// If a receive loop has not been started (no client connected), this will wait
        /// until a client connects and a message arrives.
        /// </summary>
        public async Task<MoveInfo> WaitForOpponentMove()
        {
            ThrowIfDisposed();
            return await Incoming.Reader.ReadAsync().ConfigureAwait(false);
        }

        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            var rented = BufferPool.Rent(8 * 1024);
            try
            {
                while (!ct.IsCancellationRequested && Socket is not null && Socket.State == WebSocketState.Open)
                {
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult? result;
                    do
                    {
                        result = await Socket.ReceiveAsync(rented, ct).ConfigureAwait(false);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            try { await Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).ConfigureAwait(false); } catch { }
                            return;
                        }
                        ms.Write(rented, 0, result.Count);
                    } while (!result.EndOfMessage);

                    string payload = Encoding.UTF8.GetString(ms.ToArray());
                    var move = MoveInfo.Deserialize(payload);
                    await Incoming.Writer.WriteAsync(move, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            catch (WebSocketException) { }
            finally
            {
                BufferPool.Return(rented);
                try { Incoming.Writer.TryComplete(); } catch { }
            }
        }

        private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(Disposed, this);

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;

            try { AcceptCts?.Cancel(); } catch { }
            try { Socket?.Abort(); } catch { }
            try { Socket?.Dispose(); } catch { }
            try { Listener.Stop(); } catch { }
            try { Listener.Close(); } catch { }
            AcceptCts?.Dispose();
        }
    }
}
