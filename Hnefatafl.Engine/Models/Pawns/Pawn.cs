using Hnefatafl.Engine.Enums;

namespace Hnefatafl.Engine.Models.Pawns
{
    public abstract class Pawn(Field field)
    {
        public Field Field { get; internal set; } = field;
        public abstract Side Side { get; }
    }
}
