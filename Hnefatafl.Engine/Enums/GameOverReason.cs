namespace Hnefatafl.Engine.Enums
{
    public enum GameOverReason
    {
        AllAttackerPawnsCaptured = MoveResult.AllAttackerPawnsCaptured,
        KingCaptured = MoveResult.KingCaptured,
        KingEscaped = MoveResult.KingEscaped,
    }

    public static class GameOverReasonExtension
    {
        public static Side AsWinner(this GameOverReason gameOverReason)
            => gameOverReason is GameOverReason.KingCaptured
             ? Side.Attackers
             : Side.Defenders;
    }
}
