namespace Hnefatafl.Engine.Enums
{
    public enum MoveValidationResult
    {
        Success,
        NonCurrentPlayerPawn,
        PawnAlreadyHere,
        PawnCannotMove,
        NotInLine,
        PathBlocked,
    }
}
