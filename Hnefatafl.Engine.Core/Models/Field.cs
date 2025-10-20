using Hnefatafl.Engine.Models.Pawns;

namespace Hnefatafl.Engine.Models
{
    public class Field
    {
        private const int SpawnRange = 3;

        public Coordinates Coordinates { get; }
        public bool IsEmpty => Pawn is null;
        public bool IsCorner { get; }
        public bool IsCenter { get; }
        public bool IsDefenderSpawn { get; }
        public bool IsAttackerSpawn { get; }
        public Pawn? Pawn { get; internal set; }
    
        public Field(int row, int column)
        {
            Coordinates = new(row, column);

            IsCorner = column % (Board.SIZE - 1) == 0 && row % (Board.SIZE - 1) == 0;
            IsCenter = column == Board.MIDDLE_INDEX && row == Board.MIDDLE_INDEX;

            int colFromCenter = Math.Abs(Board.MIDDLE_INDEX - column);
            int rowFromCenter = Math.Abs(Board.MIDDLE_INDEX - row);
            IsDefenderSpawn = colFromCenter + rowFromCenter < SpawnRange;
            IsAttackerSpawn = colFromCenter % Board.MIDDLE_INDEX == 0 && rowFromCenter < SpawnRange || colFromCenter % Board.MIDDLE_INDEX == Board.MIDDLE_INDEX - 1 && Board.MIDDLE_INDEX - row == 0
                           || rowFromCenter % Board.MIDDLE_INDEX == 0 && colFromCenter < SpawnRange || rowFromCenter % Board.MIDDLE_INDEX == Board.MIDDLE_INDEX - 1 && Board.MIDDLE_INDEX - column == 0;
        }

        public char GetCharRepresentation() => Pawn switch
        {
            King => 'K',
            Defender => 'D',
            Attacker => 'A',
            _ => ' '
        };

        public override string ToString() => GetCharRepresentation().ToString();

        public override bool Equals(object? obj) => obj is Field other && Coordinates == other.Coordinates;

        public override int GetHashCode() => HashCode.Combine(Coordinates.ToString());
    }
}
