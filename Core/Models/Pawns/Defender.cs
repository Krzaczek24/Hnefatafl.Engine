using Hnefatafl.Engine.Enums;

namespace Hnefatafl.Engine.Models.Pawns
{
    public class Defender(Field field) : Pawn(field)
    {
        public override Player Player => Player.Defender;
    }
}
