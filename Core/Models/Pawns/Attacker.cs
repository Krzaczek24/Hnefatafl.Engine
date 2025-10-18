using Hnefatafl.Engine.Enums;

namespace Hnefatafl.Engine.Models.Pawns
{
    public class Attacker(Field field) : Pawn(field)
    {
        public override Player Player => Player.Attacker;
    }
}
