using Hnefatafl.Engine.OnlineConnector.Models;

namespace Hnefatafl.Engine.OnlineConnector.Interfaces
{
    public interface IGameConnector : IDisposable
    {
        Task SendMyMove(MoveInfo move);
        Task<MoveInfo> WaitForOpponentMove();
    }
}
