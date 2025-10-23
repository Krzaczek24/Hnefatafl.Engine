
using Hnefatafl.Engine.Online.Interfaces;
using Hnefatafl.Engine.Online.Models;
using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace Hnefatafl.Engine.Online
{
    /// <summary>
    /// Connects to a WebSocket server and exposes simple send/receive
    /// operations for typed moves (TMove). Incoming moves are queued and
    /// can be awaited using <see cref="WaitForOpponentMove"/>.
    /// </summary>
    public sealed class GameClient(Uri serverUri) : IGameConnector
    {
        private ClientWebSocket Socket { get; } = new();
        private Uri ServerUri { get; } = serverUri ?? throw new ArgumentNullException(nameof(serverUri));
        private Channel<MoveInfo> Incoming { get; } = Channel.CreateBounded<MoveInfo>(1);
        private ArrayPool<byte> BufferPool { get; } = ArrayPool<byte>.Shared;
        private CancellationTokenSource? ReceiveCts { get; set; }
        private bool Disposed { get; set; }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (Socket.State == WebSocketState.Open) return;
            await Socket.ConnectAsync(ServerUri, cancellationToken).ConfigureAwait(false);
            ReceiveCts = new CancellationTokenSource();
            _ = Task.Run(() => ReceiveLoopAsync(ReceiveCts.Token), CancellationToken.None);
        }

        /// <summary>
        /// Send a move to connected server. This method sends the JSON text frame
        /// and returns after the frame is queued/sent; it does NOT wait for any reply.
        /// </summary>
        public async Task SendMyMove(MoveInfo move)
        {
            ThrowIfDisposed();
            if (Socket.State != WebSocketState.Open)
                throw new InvalidOperationException("Socket is not connected.");

            string text = move.Serialize();
            byte[] bytes = Encoding.UTF8.GetBytes(text);

            try
            {
                await Socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken: CancellationToken.None).ConfigureAwait(false);
            }
            catch (WebSocketException ex)
            {
                throw new IOException("Failed to send move to server.", ex);
            }
        }

        /// <summary>
        /// Waits asynchronously for the next opponent move (TMove) sent by the remote peer.
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
                while (!ct.IsCancellationRequested && Socket.State == WebSocketState.Open)
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
                    MoveInfo move = MoveInfo.Deserialize(payload);
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

            try { ReceiveCts?.Cancel(); } catch { }
            try { Socket?.Abort(); } catch { }
            try { Socket?.Dispose(); } catch { }
            ReceiveCts?.Dispose();
        }
    }
}
