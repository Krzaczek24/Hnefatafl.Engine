using Hnefatafl.Engine.Enums;

namespace Hnefatafl.Engine.Models.Pawns
{
    public class Attacker(Field field) : Pawn(field)
    {
        public override Side Side => Side.Attackers;
    }
}
