using Hnefatafl.Engine.Enums;
using Hnefatafl.Engine.Models.Pawns;
using KrzaqTools.Collections;

namespace Hnefatafl.Engine.Models
{
    public class Board(Game game) : ReadOnlyTable<Field>(GetEmptyTable())
    {
        public const int SIZE = 11;
        public const int MIDDLE_INDEX = (SIZE - 1) / 2;

        public Game Game { get; } = game;

        public Field this[Coordinates coordinates] => this[coordinates.Column.Index, coordinates.Row.Index];

        public void Reset()
        {
            foreach (Field field in this)
            {
                if (field.IsCenter)
                    field.Pawn = new King(field);
                else if (field.IsDefenderSpawn)
                    field.Pawn = new Defender(field);
                else if (field.IsAttackerSpawn)
                    field.Pawn = new Attacker(field);
                else
                    field.Pawn = null;
            }
        }

        public IEnumerable<Pawn> GetPawns(Player player)
        {
            return this
                .Where(field => field.Pawn is not null && player.HasFlag(field.Pawn.Player))
                .Select(field => field.Pawn!);
        }

        public bool CanMove(Pawn pawn)
        {
            var adjacentFields = GetAdjacentFields(pawn.Field);
            if (pawn is not King)
                adjacentFields = adjacentFields.Where(field => !field.IsCenter && !field.IsCorner);
            return adjacentFields.Any(field => field.Pawn == null);
        }

        public IEnumerable<Field> GetAdjacentFields(Field field)
        {
            var (column, row) = field.Coordinates;
            if (column > 0)
                yield return this[column - 1, row];
            if (column < SIZE - 1)
                yield return this[column + 1, row];
            if (row > 0)
                yield return this[column, row - 1];
            if (row < SIZE - 1)
                yield return this[column, row + 1];
        }

        public override int GetHashCode() => HashCode.Combine(this);

        public override bool Equals(object? obj)
        {
            if (obj is not Board other)
                return false;
            return this.SequenceEqual(other);
        }

        private static Field[,] GetEmptyTable()
        {
            var table = new Field[SIZE, SIZE];
            for (int column = 0; column < SIZE; column++)
                for (int row = 0; row < SIZE; row++)
                    table[column, row] = new Field(column, row);
            return table;
        }
    }
}
