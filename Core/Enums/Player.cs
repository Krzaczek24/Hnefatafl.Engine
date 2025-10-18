namespace Hnefatafl.Engine.Enums
{
    [Flags]
    public enum Player
    {
        None = 0,
        Attacker = 1,
        Defender = 2,
        All = Attacker | Defender
    }
}
