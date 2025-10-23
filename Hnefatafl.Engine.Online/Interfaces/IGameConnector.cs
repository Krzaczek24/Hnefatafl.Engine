using Hnefatafl.Engine.Online.Models;

namespace Hnefatafl.Engine.Online.Interfaces
{
    public interface IGameConnector : IDisposable
    {
        Task SendMyMove(MoveInfo move);
        Task<MoveInfo> WaitForOpponentMove();
    }
}
