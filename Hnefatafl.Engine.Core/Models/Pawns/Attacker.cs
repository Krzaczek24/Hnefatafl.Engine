using Hnefatafl.Engine.Enums;

namespace Hnefatafl.Engine.Models.Pawns
{
    public class Attacker(Field field) : Pawn(field)
    {
        public override Side Player => Side.Attackers;
    }
}
