using Hnefatafl.Engine.Enums;
using Hnefatafl.Engine.Models;
using Hnefatafl.Engine.Models.Pawns;

namespace Hnefatafl.Engine.Events
{
    public delegate void GameOverEventHandler(GameOverReason reason, Side winner);
    public delegate void InvalidMoveEventHandler(Pawn movingPawn, Field targetField, MoveValidationResult moveValidationResult);
    public delegate void PawnCapturedEventHandler(Pawn capturedPawn, Pawn attackingPawn, IEnumerable<Field> assistingFields);
}
