using System.Diagnostics.CodeAnalysis;

namespace Hnefatafl.Engine.Models
{
    public record Coordinates
    {
        public LabeledIndex Column { get; }
        public LabeledIndex Row { get; }

        public Coordinates(int row, int column)
        {
            Column = new(column, ((char)('A' + column)).ToString());
            Row = new(row, (row + 1).ToString());
        }

        public Coordinates(string label)
        {
            char letter = Array.Find(label.ToCharArray(), char.IsLetter);
            int number = int.Parse(label.Trim(letter));
            letter = char.ToUpper(letter);

            Column = new(letter - 'A', letter.ToString());
            Row = new(number - 1, number.ToString());
        }

        public void Deconstruct(out int row, out int column) => (row, column) = (Row.Index, Column.Index);

        public override string ToString() => $"{Column.Label}{Row.Label}";

        public static Coordinates operator +(Coordinates left, (int Rows, int Columns) right) => new(left.Row + right.Rows, left.Column + right.Columns);
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
