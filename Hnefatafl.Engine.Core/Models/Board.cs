using Hnefatafl.Engine.Enums;
using Hnefatafl.Engine.Models.Pawns;
using KrzaqTools.Collections;
using KrzaqTools.Extensions;

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

        public IEnumerable<Pawn> GetPawns(Player player, bool withMoveAvailableOnly)
        {
            var pawns = this
                .Where(field => !field.IsEmpty && player.HasFlag(field.Pawn!.Player))
                .Select(field => field.Pawn!);

            return withMoveAvailableOnly
                ? pawns.Where(CanMove)
                : pawns;
        }

        public IEnumerable<Field> GetPawnAvailableFields(Pawn pawn)
        {
            var row = this.GetRow(pawn.Field.Coordinates.Row);
            var column = this.GetColumn(pawn.Field.Coordinates.Column);

            return [
                ..GetFromPawnToSide(row),
                ..GetFromPawnToSide(column),
                ..GetFromPawnToSide(row.Reverse()),
                ..GetFromPawnToSide(column.Reverse())
            ];

            IEnumerable<Field> GetFromPawnToSide(IEnumerable<Field> first) => first
                .SkipWhile(field => field != pawn.Field)
                .Skip(1)
                .TakeWhile(field => field.IsEmpty && (!field.IsCenter && !field.IsCorner || pawn is King));
        }

        public bool CanMove(Pawn pawn)
        {
            var adjacentFields = GetAdjacentFields(pawn.Field);
            if (pawn is not King)
                adjacentFields = adjacentFields.Where(field => !field.IsCenter && !field.IsCorner);
            return adjacentFields.Any(field => field.IsEmpty);
        }

        public IEnumerable<Field> GetAdjacentFields(Field field)
        {
            var (row, column) = field.Coordinates;
            if (row > 0)
                yield return this[row - 1, column];
            if (row < SIZE - 1)
                yield return this[row + 1, column];
            if (column > 0)
                yield return this[row, column - 1];
            if (column < SIZE - 1)
                yield return this[row, column + 1];
        }

        internal static void MovePawn(Pawn pawn, Field field)
        {
            (pawn.Field.Pawn, field.Pawn) = (field.Pawn, pawn.Field.Pawn);
            pawn.Field = field;
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
            for (int row = 0; row < SIZE; row++)
                for (int column = 0; column < SIZE; column++)
                    table[row, column] = new Field(row, column);
            return table;
        }
    }
}
