using System.Diagnostics.CodeAnalysis;

namespace Hnefatafl.Engine.Models
{
    public record Coordinates
    {
        public LabeledIndex Column { get; }
        public LabeledIndex Row { get; }

        internal Coordinates(int row, int column)
        {
            Column = new(column, ((char)('A' + column)).ToString());
            Row = new(row, (row + 1).ToString());
        }

        public void Deconstruct(out int row, out int column) => (row, column) = (Row.Index, Column.Index);

        public override string ToString() => $"{Column.Label}{Row.Label}";

        public static (int rows, int columns) operator -(Coordinates left, Coordinates right) => new(left.Row - right.Row, left.Column - right.Column);

        public readonly struct LabeledIndex(int index, string label)
        {
            public int Index { get; } = index;
            public string Label { get; } = label;

            public override bool Equals([NotNullWhen(true)] object? obj) => obj is LabeledIndex other && Index == other.Index;
            public override int GetHashCode() => Index;
            public static bool operator ==(LabeledIndex left, LabeledIndex right) => left.Index == right.Index;
            public static bool operator !=(LabeledIndex left, LabeledIndex right) => !(left == right);
            public static int operator -(LabeledIndex left, LabeledIndex right) => left.Index - right.Index;
            public static implicit operator int(LabeledIndex that) => that.Index;
        }
    }
}
