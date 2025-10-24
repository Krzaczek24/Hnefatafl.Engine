using Hnefatafl.Engine.OnlineConnector.Interfaces;
using Hnefatafl.Engine.OnlineConnector.Models;
using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;

namespace Hnefatafl.Engine.OnlineConnector.Connectors.Base
{
    public abstract class GameConnectorBase<TWebSocket> : IGameConnector, IDisposable
        where TWebSocket : WebSocket
    {
        protected Channel<MoveInfo> Incoming { get; } = Channel.CreateBounded<MoveInfo>(1);
        protected ArrayPool<byte> BufferPool { get; } = ArrayPool<byte>.Shared;
        protected TWebSocket? Socket { get; set; }
        protected CancellationTokenSource? ReceiveCts { get; set; }
        private bool Disposed { get; set; }

        public virtual async Task<MoveInfo> WaitForOpponentMove()
        {
            ThrowIfDisposed();
            return await Incoming.Reader.ReadAsync().ConfigureAwait(false);
        }

        protected async Task ReceiveLoopAsync(CancellationToken ct)
        {
            var rented = BufferPool.Rent(8 * 1024);
            try
            {
                while (!ct.IsCancellationRequested && Socket?.State == WebSocketState.Open)
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
                    }
                    while (!result.EndOfMessage);

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

        protected void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(Disposed, this);

        private async Task SendTextAsync(string text)
        {
            ThrowIfDisposed();
            if (Socket?.State != WebSocketState.Open)
                throw new InvalidOperationException("Socket is not connected.");

            var bytes = Encoding.UTF8.GetBytes(text);
            try
            {
                await Socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken: CancellationToken.None).ConfigureAwait(false);
            }
            catch (WebSocketException ex)
            {
                throw new IOException("Failed to send move.", ex);
            }
        }

        public Task SendMyMove(MoveInfo move) => SendTextAsync(move.ToString());

        public virtual void Dispose()
        {
            if (Disposed) return;
            Disposed = true;

            try { ReceiveCts?.Cancel(); } catch { }
            try { Socket?.Abort(); } catch { }
            try { Socket?.Dispose(); } catch { }
            ReceiveCts?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
