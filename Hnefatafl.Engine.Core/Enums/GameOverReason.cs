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
        public static Player AsWinner(this GameOverReason gameOverReason)
            => gameOverReason is GameOverReason.KingCaptured
             ? Player.Attacker
             : Player.Defender;
    }
}
