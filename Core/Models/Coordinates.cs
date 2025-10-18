using System.Diagnostics.CodeAnalysis;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Hnefatafl.Engine.Models
{
    public record Coordinates
    {
        public LabeledIndex Column { get; }
        public LabeledIndex Row { get; }

        internal Coordinates(int column, int row)
        {
            Column = new(column, ((char)('A' + column)).ToString());
            Row = new(row, row.ToString());
        }

        public void Deconstruct(out int column, out int row) => (column, row) = (Column.Index, Row.Index);

        public override string ToString() => $"{Column.Label}{Row.Label}";

        public static (int columns, int rows) operator -(Coordinates left, Coordinates right) => new(left.Column - right.Column, left.Row - right.Row);

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
