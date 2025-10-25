using Hnefatafl.Engine.Enums;
using Hnefatafl.Engine.Models.Pawns;
using KrzaqTools.Collections;
using KrzaqTools.Extensions;

namespace Hnefatafl.Engine.Models
{
    public class Board : ReadOnlyTable<Field>
    {
        public const int SIZE = 11;
        public const int MIDDLE_INDEX = (SIZE - 1) / 2;

        internal Game Game { get; }

        public Field this[Coordinates coordinates] => this[coordinates.Row, coordinates.Column];

        public Board(Game game) : this(game, GetEmptyTable()) { }
        internal Board(Game game, Field[,] fields) : base(fields)
        {
            Game = game;
        }

        internal void Reset()
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

        public IEnumerable<Pawn> GetPawns(Side side) => this
            .Where(field => !field.IsEmpty && side.HasFlag(field.Pawn!.Side))
            .Select(field => field.Pawn!);

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

        public static bool AreValid(Coordinates coordinates)
        {
            return InRange(coordinates.Row) && InRange(coordinates.Column);
            static bool InRange(int index) => index is >= 0 and < SIZE;
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

        internal static Field[,] GetEmptyTable()
        {
            var table = new Field[SIZE, SIZE];
            for (int row = 0; row < SIZE; row++)
                for (int column = 0; column < SIZE; column++)
                    table[row, column] = new Field(row, column);
            return table;
        }
    }
}
