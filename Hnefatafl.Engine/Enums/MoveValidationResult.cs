namespace Hnefatafl.Engine.Enums
{
    public enum MoveValidationResult
    {
        Success,
        NonCurrentPlayerPawn,
        PawnAlreadyOnField,
        PawnCannotMove,
        NotInLine,
        PathBlocked,
    }
}
