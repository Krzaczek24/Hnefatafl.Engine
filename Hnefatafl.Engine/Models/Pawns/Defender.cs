using Hnefatafl.Engine.Enums;

namespace Hnefatafl.Engine.Models.Pawns
{
    public class Defender(Field field) : Pawn(field)
    {
        public override Side Side => Side.Defenders;
    }
}
