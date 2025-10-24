using Hnefatafl.Engine.Models;

namespace Hnefatafl.Engine.OnlineConnector.Models
{
    public record MoveInfo(Coordinates From, Coordinates To)
    {
        private const char SEPRARATOR = '|';

        public static string Serialize(MoveInfo moveInfo) => $"{moveInfo.From}{SEPRARATOR}{moveInfo.To}";
        public static MoveInfo Deserialize(string text)
        {
            string[] labels = text.Split(SEPRARATOR);
            return new(new(labels[0]), new(labels[1]));
        }

        public override string ToString() => Serialize(this);
    }
}
