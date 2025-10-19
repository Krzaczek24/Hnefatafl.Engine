namespace Hnefatafl.Engine.Enums
{
    [Flags]
    public enum MoveResult
    {
        None = 0,
        PawnMoved = 1 << 0,
        OpponentPawnKilled = 1 << 1 | PawnMoved,
        AttackerPawnKilled = 1 << 2 | OpponentPawnKilled,
        DefenderPawnKilled = 1 << 3 | OpponentPawnKilled,
        KingKilled = 1 << 4 | DefenderPawnKilled,
        KingEscaped = 1 << 5 | PawnMoved,
    }
}
