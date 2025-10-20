namespace Hnefatafl.Engine.Enums
{
    [Flags]
    public enum MoveResult
    {
        None = 0,
        PawnMoved = 1 << 0,
        OpponentPawnCaptured = 1 << 1 | PawnMoved,
        DefenderPawnCaptured = 1 << 2 | OpponentPawnCaptured,
        AttackerPawnCaptured = 1 << 3 | OpponentPawnCaptured,
        AllAttackerPawnsCaptured = 1 << 4 | AttackerPawnCaptured,
        KingCaptured = 1 << 5 | DefenderPawnCaptured,
        KingEscaped = 1 << 6 | PawnMoved,
    }

    public static class MoveResultExtension
    {
        public static bool IsGameOverMove(this MoveResult moveResult)
            => moveResult is MoveResult.AllAttackerPawnsCaptured
                          or MoveResult.KingCaptured
                          or MoveResult.KingEscaped;

        public static GameOverReason? AsGameOverReason(this MoveResult moveResult)
            => moveResult.IsGameOverMove()
             ? (GameOverReason)moveResult
             : null;
    }
}
